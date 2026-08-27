#!/bin/sh

set -eu

sed -E \
  -e 's/(Bearer[[:space:]]+)[A-Za-z0-9._~+\/=:-]+/\1[REDACTED]/g' \
  -e 's/("(accessToken|password|passwordConfirmation|currentPassword|newPassword|newPasswordConfirmation|passwordHash|Jwt__SigningKey|JWT_SIGNING_KEY_BASE64)"[[:space:]]*:[[:space:]]*")[^"]*"/\1[REDACTED]"/g' \
  -e 's/((accessToken|access_token|password|passwordConfirmation|currentPassword|newPassword|newPasswordConfirmation|passwordHash|Jwt__SigningKey|JWT_SIGNING_KEY_BASE64)[[:space:]]*[=:][[:space:]]*)[^&[:space:]",]+/\1[REDACTED]/g' \
  -e 's/[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}/[REDACTED_JWT]/g'
