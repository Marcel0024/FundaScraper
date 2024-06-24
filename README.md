[![Build and Publish](https://github.com/Marcel0024/FundaScraper/actions/workflows/build-and-publish-image.yaml/badge.svg?branch=main)](https://github.com/Marcel0024/FundaScraper/actions/workflows/build-and-publish-image.yaml)

# FundaScraper - New listings to webhooks (and file)

`marcel0024/funda-scraper` docker image provides the easiest way to perform web scraping on Funda, the Dutch housing website.
You simply provide the URL that you want to be scraped with the prefilled search criteria, and the image does the rest. 
You can either have webhooks to be notified about new listings (works best with something like `HomeAssistant`). Or you can review the `results.csv`.
Scraping times are set by a CRON expression, so you can set it to once a day, twice a day, etc.

What makes this scraper unique is, it imitates a real user browsing the website.
It opens a browser, loads the page, and waits for the page to load and then scrapes it. Further more you can override all selectors to make it work with future changes on the website.
That way you don't have to wait for the image to be updated.

Please note:

1. Scraping this website is ONLY allowed for personal use (as per Funda's Terms and Conditions).
2. Any commercial use of this package is prohibited. The author holds no liability for any misuse of the package.


## Docker examples

Note `--tty` and `--cap-add=SYS_ADMIN` are required.

### Docker run

```bash
docker run --tty \
    -v /data/fundascraper:/data \
    -e FUNDA_URL="https://www.funda.nl/zoeken/koop?selected_area=%5B%22amsterdam%22%5D&object_type=%5B%22house%22%5D&price=%22-450000%22" \
    -e WEBHOOK_URL="http://homeassistantlocal.ip/api/webhook/123-redacted-key" \
    ghcr.io/marcel0024/funda-scraper:latest
```

### Docker Compose

```yaml
services:
  funda-scraper:
    image: ghcr.io/marcel0024/funda-scraper:latest
    container_name: funda-scraper
    tty: true
    environment:
      - FUNDA_URL=https://www.funda.nl/zoeken/koop?selected_area=%5B%22amsterdam%22%5D&object_type=%5B%22house%22%5D&price=%22-450000%22
      - WEBHOOK_URL=http://homeassistantlocal.ip/api/webhook/123-redacted-key
    volumes:
      - /data/fundascraper:/data
```

## Environment Variables

| Variable                   | Required         | Default      | Description                                                                                                                                                                                                                                |
| -------------------------- | ---------------- | ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `CRON`                     | No (has default) | `0 7 * * *` | Every day at 7AM in the morning.                                                                                                                                                                                                           |
| `FUNDA_URL`                | Yes              | -            | The starting URL to scrape. You can build the parameters in the browser and just copy the link. Pricing, area, location, etc are all embedded in the URL, so make sure you filter it on the website before you copy it.                                                                                                                                                                      |
| `WEBHOOK_URL`              | No               | -            | The webhook URL to send the new listings to.                                                                                                                                                                                               |
| `ERROR_WEBHOOK_URL`        | No               | -            | The webhook URL to send errors to parsing fails and stops the app.                                                                                                                                                                         |
| `START_PAGE`               | No               | 1            | The page to start with (pagination)                                                                                                                                                                                                        |
| `TOTAL_PAGES`              | No               | 5            | Total pages to scrape. Increase this you're quering a big area.                                                                                                                                                                            |
| `RUN_ON_STARTUP`           | No               | `false`      | Run the crawl on startup. If `false` the next run depends on the `CRON` value.                                                                                                                                                             |
| `PAGE_CRAWL_LIMIT`         | No               | `500`        | The total pages it can crawl for each run.  Highly unlikely this needs to be edited.                                                                                                                                                       |
| `TOTAL_PARALLELISM_DEGREE` | No               | 5            | Total browsers that can be open at the same time. It's a balance with hardware specs/site limits before blocking and how fast the scraping needs to be done. These are all done within the container you won't physically see the browser. |

### Selector variables

| Variable               | Default             | Description                     |
| ---------------------- | ------------------- | ------------------------------- |
| `LISTING_SELECTOR`     | See `FundaScraper/defaults.json` | The selector to click a listing |
| `TITLE_SELECTOR`       | See `FundaScraper/defaults.json` | The selector for the address    |
| `ZIP_CODE_SELECTOR`    | See `FundaScraper/defaults.json` | The selector for the zipcode    |
| `PRICE_SELECTOR`       | See `FundaScraper/defaults.json` | The selector for the price      |
| `AREA_SELECTOR`        | See `FundaScraper/defaults.json` | The selector for the area       |
| `TOTAL_ROOMS_SELECTOR` | See `FundaScraper/defaults.json` | The selector for total rooms    |


# Webhook object
A post is done to the WEBHOOK_URL for each listing with the following JSON object:

```json
 {
  "name": "Lorem Ipsum",
  "price": "€ 12334",
  "zipCode": "1234",
  "area": "100 m²",
  "totalRooms": "4",
  "url": "https://funda.nl/koop/#example-link"
 }
```

## HomeAssistant webhook endpoint example

```yaml
alias: "Funda Alerts"
trigger:
  - platform: webhook
    allowed_methods:
      - POST
    local_only: true
    webhook_id: "123-redacted-key" # Replace with your own
action:
  - service: notify.mobile_app_android # Replace with your own
    data:
      title: Funda Alert
      message: "{{ trigger.json.title }} {{ trigger.json.zipCode }} is te koop voor {{ trigger.json.price }}"
      data:
        clickAction: "{{ trigger.json.url }}"
mode: single
```


## Troubleshoot/Common issues

### UnauthorizedAccessException: Access to the path '/data/results.csv' is denied

The app inside the container is running as non-root user. So the application is running as the predefined `app` user which has UID 64198. 

For it to be able to create files in the mounted directory, UID 64198 needs to be able to create files on the host in the `/data/fundascraper` directory (the one defined in the volume).

You can do that by giving public write access on the host using `chmod o+w /data/fundascraper`.

If that's too permissive, you can create a user on the host with UID 64198 and give that user group access to the directory.

