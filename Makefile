.PHONY: deps fmt lint build
deps:
	dotnet restore
fmt:
	dotnet format
lint:
	dotnet format --verify-no-changes --severity warn
build:
	dotnet build -c Release
