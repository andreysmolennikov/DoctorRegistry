FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/DoctorRegistry.Api/DoctorRegistry.Api.csproj", "src/DoctorRegistry.Api/"]
COPY ["src/DoctorRegistry.Application/DoctorRegistry.Application.csproj", "src/DoctorRegistry.Application/"]
COPY ["src/DoctorRegistry.Domain/DoctorRegistry.Domain.csproj", "src/DoctorRegistry.Domain/"]
COPY ["src/DoctorRegistry.Infrastructure/DoctorRegistry.Infrastructure.csproj", "src/DoctorRegistry.Infrastructure/"]
RUN dotnet restore "src/DoctorRegistry.Api/DoctorRegistry.Api.csproj"
COPY . .
WORKDIR "/src/src/DoctorRegistry.Api"
RUN dotnet publish "DoctorRegistry.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DoctorRegistry.Api.dll"]
