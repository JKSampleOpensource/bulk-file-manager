# Bulk File Manager

This is a desktop-based tool for moving files between a local computer and ACC Docs, BIM 360 Docs, and BIM 360 Team. 
It may be used both to upload files, and to download files.

## Table of Contents
<!-- TOC -->
* [Bulk File Manager](#bulk-file-manager)
  * [Table of Contents](#table-of-contents)
  * [Pre-requisites](#pre-requisites)
  * [Usage](#usage)
    * [Starting the Application](#starting-the-application)
    * [Authentication Modes](#authentication-modes)
      * [Using App-based permissions](#using-app-based-permissions)
      * [Using User-based permissions](#using-user-based-permissions)
    * [Downloading files from BIM 360 Docs, ACC Docs, and BIM 360 Team](#downloading-files-from-bim-360-docs-acc-docs-and-bim-360-team)
    * [Viewing Download Progress](#viewing-download-progress)
    * [Uploading files to BIM 360 Docs or ACC Docs](#uploading-files-to-bim-360-docs-or-acc-docs)
      * [Additional Options](#additional-options)
    * [Viewing Upload Progress](#viewing-upload-progress)
<!-- TOC -->

## Pre-requisites

Before the tool can be used, you must have the following: 
* [.NET 6 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
* [Webview2 Evergreen Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section)
* An APS application Client ID and Secret with Data Management permissions, that has been added to the relevant BIM 
  360 or ACC Account, and with a callback URL of `http://localhost:8083/code`.
* A user account with Document Management permission to the files in question.

## Usage

### Starting the Application

1. Unzip the release package, and run the `Bulk-File-Manager.exe` file.
2. Add the Client ID and Secret, and click `Save & Log In`.
3. Complete the sign-in process with your Autodesk account. 

### Authentication Modes

The application supports two authentication modes: App-based (2 Legged) and User-based (3 Legged). 

App based permissions will allow you to work with any files in any Autodesk Hubs that the Client ID has access to. 
In most cases, this will be the preferred method.

User based permissions will limit you to working with files that your user account has access to. This is also 
required for downloading files from BIM 360 Team, as it does not support App-based permissions.

#### Using App-based permissions
1. Go to the Create Upload, Create Download, or Settings page
2. Press the App (2 Legged) button at the top of the page

#### Using User-based permissions
1. Go to the Create Upload, Create Download, or Settings page
2. Press the User (3 Legged) button at the top of the page
3. Go through the log in flow in the pop-up window
4. When the log in window closes, press the 'sync' button to the right of the 'User (3 Legged)' button.

### Downloading files from BIM 360 Docs, ACC Docs, and BIM 360 Team

1. Click 'Create Download' at the top of the page
2. To download a single project or folder:
   1. Click `Browse Cloud Location`
   2. Select the account and project from the dropdown
   3. Select the folder from the tree view
   4. Click `Finish` to close the popup
   5. Select the source folder by clicking the folder icon in the `Local Folder Path` field.
   6. Press `Download Files` to start the download
3. To download multiple projects or folders:
   1. Select the source folder by clicking the folder icon in the `Local Folder Path` field.
   2. Click `Bulk Download`
   3. Click `Get all hubs and projects`
   4. Wait until the Excel file is downloaded
   5. Open the Excel file and remove any projects that you don't want to download
   6. Click `Create Bulk Download Jobs`
   7. Select the excel file

### Viewing Download Progress

1. Click `Download History` at the top of the page
2. The list will automatically update every 15 seconds, but may also be refreshed by pressing the `Sync` button at 
   the top right of the page.
3. To view the details of a group of downloading files, press the colored button with the number of files.
4. To restart a failed download, select the files you with to repeat and press `Retry Selected`

### Uploading files to BIM 360 Docs or ACC Docs

1. Click 'Create Upload' at the top of the page
2. Select the target folder by clicking the folder icon in the `Local Folder Path` field.
3. Click `Browse Cloud Location`
2. Select the account and project from the dropdown
3. Select the folder from the tree view
4. Click `Finish` to close the popup
5. To see what files and folders would be uploaded, press `Create Dry Run`
6. To upload the files, press `Upload Files`

#### Additional Options

Using the Advanced Options, you may select files types to exclude, folder names to exclude, or write custom 
Javascript to determine which files should be uploaded or downloaded.

### Viewing Upload Progress

1. Click `Upload History` at the top of the page
2. The list will automatically update every 15 seconds, but may also be refreshed by pressing the `Sync` button at 
   the top right of the page.
3. To view the details of a group of uploading files, press the colored button with the number of files.
4. To restart a failed upload, select the files you with to repeat and press `Retry Selected`
5. To view the details of a bulk upload, click the row of the Bulk Upload you wish to view.