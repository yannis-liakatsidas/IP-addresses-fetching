# IP Address Fetching .NET Application

## Overview
This .NET application is designed to manage and retrieve IP address information efficiently. It utilizes a relational database, in-memory caching, and an external service (ip2c.org) to ensure accurate and up-to-date IP address country data.

## Features
- **Relational Database**: Stores IP addresses along with their corresponding country information.
- **In-Memory Caching**: Quickly fetches IP address data to improve performance.
- **External Service Integration**: Makes HTTP requests to ip2c.org for IP address information when not available in cache or database.
- **Scheduled Updates**: Checks for changes in IP address information every hour and updates the database and cache accordingly.

## Installation

### Prerequisites
- .NET SDK
- SQL Database (written and tested with Microsft SQL Server)

### Steps
1. **Clone the repository**
   ```sh
   git clone https://github.com/yannis-liakatsidas/IP-addresses-fetching.git
   cd IP-addresses-fetching
