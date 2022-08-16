#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Services/ordering/Ordering.API/Ordering.API.csproj", "Services/ordering/Ordering.API/"]
COPY ["Services/ordering/Ordering.Application/Ordering.Application.csproj", "Services/ordering/Ordering.Application/"]
COPY ["Services/ordering/Ordering.Domain/Ordering.Domain.csproj", "Services/ordering/Ordering.Domain/"]
COPY ["Services/ordering/Ordering.Infrastructure/Ordering.Infrastructure.csproj", "Services/ordering/Ordering.Infrastructure/"]
RUN dotnet restore "Services/ordering/Ordering.API/Ordering.API.csproj"
COPY . .
WORKDIR "/src/Services/ordering/Ordering.API"
RUN dotnet build "Ordering.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Ordering.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ordering.API.dll"]