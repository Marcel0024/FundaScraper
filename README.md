# FundaScraper - New listings to webhooks

`funda-scraper` docker image provides the easiest way to perform web scraping on Funda, the Dutch housing website.
Works best if you have a service like Home Assistant instance to send the new listings to via webhooks.

This app can be used to monitor any Funda URL, such as a search query or a specific house listing.

Please note:

1. Scraping this website is ONLY allowed for personal use (as per Funda's Terms and Conditions).
2. Any commercial use of this package is prohibited. The author holds no liability for any misuse of the package.

## Docker examples
### Docker run

```bash
docker run --tty \
    -v c:/data/fundascraper:/data \
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
      - c:/data/fundascraper:/data
```

## Environment Variables

| Variable              | Required         | Default | Description                                                                          |
| --------------------- | ---------------- | ------- | ------------------------------------------------------------------------------------ |
| `FUNDA_URL`           | Yes              | -       | The Funda URL to monitor. You can just copy this from the browser.                   |
| `WEBHOOK_URL`         | Yes              | -       | The webhook URL to send the new listings to.                                         |
| `ERROR_WEBHOOK_URL`   | No (Recommended) | -       | The webhook URL to send errors to parsing fails and stops the app.                   |
| `INTERVAL_IN_MINUTES` | No               | `60`    | The interval in minutes to crawl. Don't overdue this - the higher the better.        |
| `PAGE_CRAWL_LIMIT`    | No               | `20`    | The total pages it can crawl for each run.                                           |
| `RUN_ON_STARTUP`      | No               | `true`  | Run the crawl on startup. If `false` the next run is in `interval-in-minutes` value. |

# Webhook object
A post is done to the WEBHOOK_URL for each listing with the following JSON object:

```json
 {
  "name": "White House 123",
  "price": "€ 12334",
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
      message: "{{ trigger.json.name }} is te koop voor {{ trigger.json.price }}"
      data:
        clickAction: "{{ trigger.json.url }}"
mode: single
```
