FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY *.sln ./
COPY .config/ ./.config/
COPY src/ ./src/
RUN dotnet tool restore
RUN dotnet restore src/ExpenseTracker.Api/ExpenseTracker.Api.csproj
RUN dotnet publish src/ExpenseTracker.Api/ExpenseTracker.Api.csproj \
    -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ExpenseTracker.Api.dll"]
