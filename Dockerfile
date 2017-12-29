FROM microsoft/dotnet:sdk AS Build
WORKDIR /
COPY ./ ./
RUN dotnet restore
RUN dotnet publish -c Release -o ./Publish