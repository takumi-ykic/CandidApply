# CandidApply
Job Application management system.  
[Click to move CandidApply deploying in Azure App Service.](https://candidapply20240514120238.azurewebsites.net/)

## Application overview
CandidApply is the simple job application management system. This has fundamental functions to manage job applications easily,
which are adding a new record, updating progress, stroing interview information, uploading resume and cover letter, and 
downloading them. The purpose of this application is that offering the simple way to manage job applications.

## Tech Stack
- ASP.NET Core
- MVC for application management
- Entity Framework Core
- Identity Framework
- MicroSoft SQL Server
- JavaScript(jQuery)
- HTML5
- CSS3
- Boostrap
- Azure App Service
- Azure SQL Database
- Azure Blob Storage

## Functionality
- Add a new application
- Update an application status(Apply, Interview, Offer, Hire, Reject). User can update status as needed.
- Upload resume and cover letter for each application and as template of them.
- Search applications in progress or in past.

## Deployment
- Azure App Service, it is used to deploy ASP.NET Core application.
- Azure SQL Database, it is used to enable this application access to Database in MicroSoft SQL Server.
- Azure Blob Storage, it it used to store and retrieve resume and cover letter as needed.
