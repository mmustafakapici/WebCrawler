# WebCrawler
Multi-Threaded Web Crawler


Web Crawler Application
This application is a web crawler that scrapes data from specific websites by crawling them. It crawls web pages using the provided URLs, saves their HTML content, and discovers new links.

Features
The application can crawl up to 3 websites simultaneously.
Each website has its own crawler manager.
Users can input website URLs and start the crawlers.
The started crawlers crawl the websites, save the HTML contents, and discover new links.
The found URLs and crawler logs are displayed in the user interface and saved to JSON files.
The crawler logs include the download time, file name, and other information of the crawled URLs.
Errors and exceptions are displayed in the user interface and stored in a separate JSON file.
Installation
Download or clone the source code of this project.
Go to the project folder and open the WebCrawler.sln file.
Build the project to install the required NuGet packages.
Usage
Launch the application.
Enter the URL of the website to be crawled in the URL input field.
Add the URL to the crawler by clicking the "Add" button.
Start the crawler by clicking the "Start" button.
Pause and resume the crawler using the "Pause" and "Resume" buttons.
The logs and found URLs will be displayed in the user interface.
Click the "Save" button to save the logs and found URLs to JSON files.
Dependencies
This project uses the following dependencies:

HtmlAgilityPack: Used for processing HTML contents.
Newtonsoft.Json: Used for JSON serialization and deserialization.
Microsoft.EntityFrameworkCore: Used for database operations.
MongoDB.Driver: Used for MongoDB database integration.
Contributing
To contribute to this project, follow these steps:

Fork this project.
Create your feature branch: git checkout -b feature/your-feature
Make your changes and commit them: git commit -am 'Add some feature'
Push to your branch: git push origin feature/your-feature
Submit a pull request to the forked repository.
License
This project is licensed under the MIT License. See the LICENSE file for more information.

