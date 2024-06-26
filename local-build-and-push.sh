#!/bin/bash

set -e

image_name="192.168.2.21:5000/marcel0024/funda-scraper:latest"

dotnet publish ./FundaScraper/FundaScraper.csproj -c Release -o publish

docker build --tag $image_name --file ./FundaScraper/Dockerfile .
docker image push $image_name