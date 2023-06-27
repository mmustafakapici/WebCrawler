# WebCrawler
Multi-Threaded Web Crawler

# Web Crawler Application

![Web Crawler Application]

## Overview

This application is a web crawler that scrapes data from specific websites by crawling them. It crawls web pages using the provided URLs, saves their HTML content, and discovers new links.

## Features

- The application can crawl up to 3 websites simultaneously.
- Each website has its own crawler manager.
- Users can input website URLs and start the crawlers.
- The started crawlers crawl the websites, save the HTML contents, and discover new links.
- The found URLs and crawler logs are displayed in the user interface and saved to JSON files.
- The crawler logs include the download time, file name, and other information of the crawled URLs.
- Errors and exceptions are displayed in the user interface and stored in a separate JSON file.

## Installation

1. Download or clone the source code of this project.
2. Go to the project folder and open the `WebCrawler.sln` file.
3. Build the project to install the required NuGet packages.

## Usage

1. Launch the application.
2. Enter the URL of the website to be crawled in the URL input field.
3. Add the URL to the crawler by clicking the "Add" button.
4. Start the crawler by clicking the "Start" button.
5. Pause and resume the crawler using the "Pause" and "Resume" buttons.
6. The logs and found URLs will be displayed in the user interface.
7. Click the "Save" button to save the logs and found URLs to JSON files.

## Dependencies

This project uses the following dependencies:

- HtmlAgilityPack: Used for processing HTML contents.
- Newtonsoft.Json: Used for JSON serialization and deserialization.
- Microsoft.EntityFrameworkCore: Used for database operations.
- MongoDB.Driver: Used for MongoDB database integration.

## Contributing

To contribute to this project, follow these steps:

1. Fork this project.
2. Create your feature branch: `git checkout -b feature/your-feature`
3. Make your changes and commit them: `git commit -am 'Add some feature'`
4. Push to your branch: `git push origin feature/your-feature`
5. Submit a pull request to the forked repository.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more information.

