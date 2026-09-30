FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the whole src folder so project references between siblings resolve correctly.
COPY src/ ./src/

WORKDIR /src/src/Timesheet.Web
RUN dotnet restore "Timesheet.Web.csproj"
RUN dotnet publish "Timesheet.Web.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Railway assigns the listening port via the PORT env var at runtime; default to 8080 for local/docker run.
ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT} dotnet Timesheet.Web.dll"]
