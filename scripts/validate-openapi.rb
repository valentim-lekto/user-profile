#!/usr/bin/env ruby
# frozen_string_literal: true

require "yaml"

def assert_contract(condition, message)
  abort message unless condition
end

def resolve_reference(document, reference)
  assert_contract reference.is_a?(String) && reference.start_with?("#/"),
                  "Only local references are supported: #{reference.inspect}"

  reference.delete_prefix("#/").split("/").reduce(document) do |current, token|
    decoded = token.gsub("~1", "/").gsub("~0", "~")
    assert_contract current.is_a?(Hash) && current.key?(decoded),
                    "Broken reference: #{reference}"
    current.fetch(decoded)
  end
end

def resolve_object(document, value)
  value.is_a?(Hash) && value.key?("$ref") ? resolve_reference(document, value.fetch("$ref")) : value
end

default_contract_path = File.expand_path("../docs/sdd/03-api-contract.yaml", __dir__)
contract_path = File.expand_path(ARGV.fetch(0, default_contract_path), Dir.pwd)
document = YAML.safe_load(File.read(contract_path), aliases: false)

assert_contract document.is_a?(Hash), "Expected an OpenAPI object"
assert_contract document["openapi"] == "3.0.3", "Expected OpenAPI 3.0.3"
assert_contract document.dig("info", "title") == "User Profile API", "Unexpected API title"
assert_contract document.dig("info", "version") == "1.0.0", "Unexpected API version"
assert_contract document.fetch("servers") == [{ "url" => "/", "description" => "Mesma origem servida pelo Nginx" }],
                "Expected the same-origin server"

expected_operations = {
  ["/api/auth/register", "post"] => {
    "operationId" => "registerUser", "security" => [],
    "requestSchema" => "RegisterRequest", "success" => ["201", "MessageResponse"],
    "responses" => %w[201 400 409 413 415 500 503]
  },
  ["/api/auth/login", "post"] => {
    "operationId" => "loginUser", "security" => [],
    "requestSchema" => "LoginRequest", "success" => ["200", "LoginResponse"],
    "responses" => %w[200 400 413 415 500 503]
  },
  ["/api/profile", "get"] => {
    "operationId" => "getCurrentProfile", "security" => [{ "bearerAuth" => [] }],
    "requestSchema" => nil, "success" => ["200", "ProfileResponse"],
    "responses" => %w[200 401 404 500 503]
  },
  ["/api/profile", "put"] => {
    "operationId" => "updateCurrentProfile", "security" => [{ "bearerAuth" => [] }],
    "requestSchema" => "UpdateProfileRequest", "success" => ["200", "ProfileResponse"],
    "responses" => %w[200 400 401 404 409 413 415 500 503]
  },
  ["/api/profile/password", "put"] => {
    "operationId" => "changeCurrentPassword", "security" => [{ "bearerAuth" => [] }],
    "requestSchema" => "ChangePasswordRequest", "success" => ["200", "MessageResponse"],
    "responses" => %w[200 400 401 404 413 415 500 503]
  },
  ["/health", "get"] => {
    "operationId" => "getHealth", "security" => [],
    "requestSchema" => nil, "success" => ["200", "HealthResponse"],
    "responses" => %w[200 500 503]
  }
}.freeze

http_methods = %w[get put post delete options head patch trace].freeze
paths = document.fetch("paths")
actual_operation_keys = paths.flat_map do |path, item|
  assert_contract item.is_a?(Hash), "Expected a path item for #{path}"
  item.keys.filter_map { |method| [path, method] if http_methods.include?(method) }
end

assert_contract actual_operation_keys.sort == expected_operations.keys.sort,
                "Unexpected path or HTTP method set"

expected_operations.each do |(path, method), expected|
  operation = paths.fetch(path).fetch(method)
  label = "#{method.upcase} #{path}"

  assert_contract operation["operationId"] == expected.fetch("operationId"),
                  "Unexpected operationId for #{label}"
  assert_contract operation["security"] == expected.fetch("security"),
                  "Unexpected security for #{label}"
  assert_contract operation.fetch("responses").keys.sort == expected.fetch("responses").sort,
                  "Unexpected responses for #{label}"

  request_schema = expected.fetch("requestSchema")
  if request_schema
    assert_contract operation.dig("requestBody", "required") == true,
                    "Request body must be required for #{label}"
    actual_request_reference = operation.dig("requestBody", "content", "application/json", "schema", "$ref")
    assert_contract actual_request_reference == "#/components/schemas/#{request_schema}",
                    "Unexpected request schema for #{label}"
  else
    assert_contract !operation.key?("requestBody"), "Unexpected request body for #{label}"
  end

  success_status, success_schema = expected.fetch("success")
  actual_success_reference = operation.dig(
    "responses", success_status, "content", "application/json", "schema", "$ref"
  )
  assert_contract actual_success_reference == "#/components/schemas/#{success_schema}",
                  "Unexpected success schema for #{label} #{success_status}"

  if path.start_with?("/api/profile")
    parameters = operation.fetch("parameters", []).map { |parameter| resolve_object(document, parameter) }
    assert_contract parameters.none? { |parameter| parameter.fetch("name", "").casecmp?("userId") },
                    "Profile operation accepts userId: #{label}"
  end
end

request_expectations = {
  "RegisterRequest" => {
    "required" => %w[name email password passwordConfirmation],
    "rules" => {
      "name" => { "type" => "string", "x-trim" => true, "x-min-length-after-trim" => 3, "x-max-length-after-trim" => 200 },
      "email" => { "type" => "string", "pattern" => '^\s*[\x21-\x3F\x41-\x7E]+@[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)+\s*$', "x-trim" => true, "x-min-length-after-trim" => 1, "x-max-length-after-trim" => 320, "x-pattern-after-trim" => '^[\x21-\x3F\x41-\x7E]+@[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)+$' },
      "password" => { "type" => "string", "format" => "password", "minLength" => 6, "maxLength" => 128, "writeOnly" => true },
      "passwordConfirmation" => { "type" => "string", "format" => "password", "minLength" => 6, "maxLength" => 128, "writeOnly" => true }
    }
  },
  "LoginRequest" => {
    "required" => %w[email password],
    "rules" => {
      "email" => { "type" => "string", "pattern" => '^\s*[\x21-\x3F\x41-\x7E]+@[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)+\s*$', "x-trim" => true, "x-min-length-after-trim" => 1, "x-max-length-after-trim" => 320, "x-pattern-after-trim" => '^[\x21-\x3F\x41-\x7E]+@[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)+$' },
      "password" => { "type" => "string", "format" => "password", "minLength" => 1, "maxLength" => 128, "writeOnly" => true }
    }
  },
  "UpdateProfileRequest" => {
    "required" => %w[name email],
    "rules" => {
      "name" => { "type" => "string", "x-trim" => true, "x-min-length-after-trim" => 3, "x-max-length-after-trim" => 200 },
      "email" => { "type" => "string", "pattern" => '^\s*[\x21-\x3F\x41-\x7E]+@[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)+\s*$', "x-trim" => true, "x-min-length-after-trim" => 1, "x-max-length-after-trim" => 320, "x-pattern-after-trim" => '^[\x21-\x3F\x41-\x7E]+@[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)+$' }
    }
  },
  "ChangePasswordRequest" => {
    "required" => %w[currentPassword newPassword newPasswordConfirmation],
    "rules" => {
      "currentPassword" => { "type" => "string", "format" => "password", "minLength" => 1, "maxLength" => 128, "writeOnly" => true },
      "newPassword" => { "type" => "string", "format" => "password", "minLength" => 6, "maxLength" => 128, "writeOnly" => true },
      "newPasswordConfirmation" => { "type" => "string", "format" => "password", "minLength" => 6, "maxLength" => 128, "writeOnly" => true }
    }
  }
}.freeze

forbidden_request_rules = {
  ["RegisterRequest", "name"] => %w[minLength maxLength],
  ["RegisterRequest", "email"] => %w[format minLength maxLength],
  ["LoginRequest", "email"] => %w[format minLength maxLength],
  ["UpdateProfileRequest", "name"] => %w[minLength maxLength],
  ["UpdateProfileRequest", "email"] => %w[format minLength maxLength]
}.freeze

request_expectations.each do |schema_name, expected|
  schema = document.dig("components", "schemas", schema_name)
  assert_contract schema.is_a?(Hash), "Missing schema #{schema_name}"
  assert_contract schema["type"] == "object", "#{schema_name} must be an object"
  assert_contract schema["additionalProperties"] == false,
                  "#{schema_name} must reject additional properties"
  assert_contract schema.fetch("required").sort == expected.fetch("required").sort,
                  "Unexpected required fields for #{schema_name}"
  assert_contract schema.fetch("properties").keys.sort == expected.fetch("rules").keys.sort,
                  "Unexpected properties for #{schema_name}"

  expected.fetch("rules").each do |property_name, rules|
    property = schema.fetch("properties").fetch(property_name)
    rules.each do |rule, value|
      assert_contract property[rule] == value,
                      "Unexpected #{rule} for #{schema_name}.#{property_name}"
    end

    forbidden_request_rules.fetch([schema_name, property_name], []).each do |rule|
      assert_contract !property.key?(rule),
                      "#{schema_name}.#{property_name} must model #{rule} after trim, not on the raw JSON value"
    end
  end

  assert_contract schema.fetch("properties").keys.none? { |name| name.casecmp?("userId") },
                  "#{schema_name} must not accept userId"
end

response_expectations = {
  "MessageResponse" => {
    "message" => { "type" => "string" }
  },
  "LoginResponse" => {
    "accessToken" => { "type" => "string", "readOnly" => true }
  },
  "ProfileResponse" => {
    "name" => { "type" => "string", "minLength" => 3, "maxLength" => 200 },
    "email" => { "type" => "string", "format" => "email", "pattern" => '^[\x21-\x3F\x41-\x7E]+@[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)+$', "maxLength" => 320 }
  },
  "HealthResponse" => {
    "status" => { "type" => "string", "enum" => ["Healthy"] }
  }
}.freeze
sensitive_response_fields = %w[id userId password passwordHash normalizedEmail accessTokenHash].freeze

response_expectations.each do |schema_name, expected_rules|
  schema = document.dig("components", "schemas", schema_name)
  assert_contract schema.is_a?(Hash), "Missing schema #{schema_name}"
  assert_contract schema["type"] == "object", "#{schema_name} must be an object"
  assert_contract schema["additionalProperties"] == false,
                  "#{schema_name} must reject additional properties"
  assert_contract schema.fetch("required").sort == expected_rules.keys.sort,
                  "Unexpected required fields for #{schema_name}"
  actual_properties = schema.fetch("properties").keys
  assert_contract actual_properties.sort == expected_rules.keys.sort,
                  "Unexpected properties for #{schema_name}"
  assert_contract (actual_properties & sensitive_response_fields).empty?,
                  "Sensitive field exposed by #{schema_name}"

  expected_rules.each do |property_name, rules|
    property = schema.fetch("properties").fetch(property_name)
    rules.each do |rule, value|
      assert_contract property[rule] == value,
                      "Unexpected #{rule} for #{schema_name}.#{property_name}"
    end
  end
end

bearer = document.dig("components", "securitySchemes", "bearerAuth")
assert_contract bearer.is_a?(Hash) && bearer["type"] == "http" && bearer["scheme"] == "bearer" &&
                bearer["bearerFormat"] == "JWT",
                "Unexpected bearerAuth definition"

expected_operations.each_key do |path, method|
  operation = paths.fetch(path).fetch(method)
  operation.fetch("responses").each do |status, response_or_reference|
    next if status.to_i < 400

    response = resolve_object(document, response_or_reference)
    content = response.fetch("content")
    assert_contract content.keys == ["application/problem+json"],
                    "#{method.upcase} #{path} #{status} must use application/problem+json"
    problem_schema = content.fetch("application/problem+json").fetch("schema")
    assert_contract problem_schema["$ref"]&.start_with?("#/components/schemas/") == true,
                    "#{method.upcase} #{path} #{status} must reference a ProblemDetails schema"
    problem_schema_name = problem_schema.fetch("$ref").split("/").last
    assert_contract %w[ProblemDetails ValidationProblemDetails].include?(problem_schema_name),
                    "#{method.upcase} #{path} #{status} must use ProblemDetails"

    next unless status == "401"

    challenge = response.dig("headers", "WWW-Authenticate")
    assert_contract challenge.is_a?(Hash) && challenge["required"] == true,
                    "#{method.upcase} #{path} 401 must require WWW-Authenticate"
    assert_contract challenge.dig("schema", "pattern") == "^Bearer(?: .*)?$",
                    "#{method.upcase} #{path} has an invalid Bearer challenge"
  end
end

assert_contract paths.dig("/api/auth/login", "post", "responses", "400", "$ref") ==
                "#/components/responses/LoginValidationProblem",
                "Invalid login must use 400 LoginValidationProblem"

references = []
walk = lambda do |value|
  case value
  when Hash
    value.each do |key, child|
      references << child if key == "$ref"
      walk.call(child)
    end
  when Array
    value.each { |child| walk.call(child) }
  end
end
walk.call(document)
references.each { |reference| resolve_reference(document, reference) }

puts "OpenAPI OK: SPEC-OAS-001..005, #{expected_operations.length} operations, #{references.length} local references"
