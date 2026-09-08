.PHONY: deps fmt lint build test
deps:
	dotnet restore
fmt:
	dotnet format
lint:
	dotnet format --verify-no-changes --severity warn
build:
	dotnet build -c Release
test:
	dotnet test -c Release --no-build
