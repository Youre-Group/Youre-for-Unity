# Changelog
All notable changes to this project will be documented in this file.

## [Unreleased]

## [0.1.0] - 2023-05-08

### Added

- Basic SignIn functionality

## [1.2.11] - 2024-06-11

### Fix

- Windows Webview executable file will not be copied on other platforms anymore

## [1.3.0] - 2024-06-11

### Added

- Token will now be refreshed on AuthenticateAsync

## [1.3.1] - 2024-06-18

### Fix

- Fixed a bug that sometimes returned null user object on authenticate

## [1.3.2] - 2024-07-02

### Fix

- Fixed register form errors on android  

## [2.0.0] - 2024-11-06

### Breaking Changes

- YOURE.ID based now on keycloak openid connect 
- New ClientID and URI are needed!
- Webview mechanics were replaced with external browser

## [2.0.1] - 2024-11-19

- Removed dll to resolve conflict. Can be a problem later with other projects
- 
## [2.0.3] - 2024-11-19

- Added AccessToken to user object.
- 
## [2.0.4] - 2024-11-21

- Reworked android to avoid activity switching