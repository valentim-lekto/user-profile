#!/usr/bin/env ruby
# frozen_string_literal: true

require "yaml"

contract_path = File.expand_path("../docs/sdd/03-api-contract.yaml", __dir__)
document = YAML.safe_load(File.read(contract_path), aliases: false)

abort "Expected OpenAPI 3.0.3" unless document["openapi"] == "3.0.3"

expected_paths = %w[
  /api/auth/register
  /api/auth/login
  /api/profile
  /api/profile/password
  /health
].sort

paths = document.fetch("paths")
abort "Unexpected path set" unless paths.keys.sort == expected_paths

http_methods = %w[get post put patch delete]
operations = paths.flat_map do |path, item|
  item.filter_map do |method, operation|
    next unless http_methods.include?(method)

    [path, method, operation]
  end
end

operation_ids = operations.map { |_, _, operation| operation.fetch("operationId") }
abort "Expected six operations" unless operations.length == 6
abort "operationId values must be unique" unless operation_ids.uniq.length == operation_ids.length

operations.each do |path, method, operation|
  expected_security = path.start_with?("/api/profile") ? [{ "bearerAuth" => [] }] : []
  abort "Unexpected security for #{method.upcase} #{path}" unless operation["security"] == expected_security
  abort "Missing responses for #{method.upcase} #{path}" if operation.fetch("responses").empty?
end

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

references.each do |reference|
  abort "Only local references are supported: #{reference}" unless reference.start_with?("#/")

  reference.delete_prefix("#/").split("/").reduce(document) do |current, token|
    decoded = token.gsub("~1", "/").gsub("~0", "~")
    abort "Broken reference: #{reference}" unless current.is_a?(Hash) && current.key?(decoded)

    current.fetch(decoded)
  end
end

puts "OpenAPI OK: #{operations.length} operations, #{references.length} local references"
