# Clean Architecture WebAPI Template for .NET Core 9 - README

## Introduction

Welcome to the Clean Architecture WebAPI Template for .NET Core 9! This template is designed to provide a solid starting point for building scalable and maintainable web applications using the .NET Core framework and ASP NET Core. Tailored for .NET Core 9, it incorporates best practices and design patterns to kickstart your project.

This project serves as a template for creating a Web API focused on microservices architecture. It provides flexibility for use with different databases, including SQL Server and MongoDB, with fully implemented, unique configurations for each, selectable by parameters.

## Description

This template adheres to the principles of Clean Architecture, ensuring separation of concerns and dependency inversion. Key features include:

- **Minimal Api**: A very easy and low code .
- **Result Pattern**: This pattern encapsulates operation outcomes in C#, indicating success or failure with associated data or errors. It simplifies error handling and improves code clarity, especially in API responses.
- **Clean Architecture**: Organized into clear layers, the project structure promotes clean separation of concerns.
- **JWT Auth**: Authentication and authorization already configured for the project, just need to set up environment vars on your machine.
- **Functional, Integration & Unitary Testing**: Robust testing setup to ensure code reliability and ease of maintenance.
- **Migrations**: Code first implementation in the SQL Server variant of this template, used Migrations also in functional testing.
- **ULID Sql**: ULID Id to provide faster, non sequential and fragmentation preventive implementation.

### Libraries and Frameworks

The project leverages several high-quality libraries and frameworks, including:
- **MediatR**: For implementing the CQRS pattern.
- **FluentValidation**: For building strongly-typed validation rules.
- **EntityFramework**: For ORM capabilities, implemented for SQL Server and soon MongoDB.
- **XUnit**: For comprehensive testing frameworks.
- **TestContainers**: For creating throwaway instances of databases inside the testing layer.
- **Swashbuckle Swagger**: For Web API documentation.

## About the Author

Hi, my name is Theo, a seasoned .NET Core backend software developer. With extensive experience in developing robust and efficient backend systems, I have integrated a wealth of best practices and tools into this template. The architectures and patterns used reflects a commitment to high-quality software development.


# Installation

## First steps

To create your own project using this template, follow these steps:

1. Clone this repository to your computer.

2. Use the following command in a console, located in the project folder, to install the template.

    ```console
    dotnet new install .
    ```

    **NOTE:** If you previously installed the template but need to install a newer version, use the `--force` parameter, or [uninstall](#Uninstalling-the-template) and then reinstall.

3. Check the installation by running the following command and looking for the `CleanWebApiTemplate` template.

    ```console
    dotnet new list
    ```

    Alternatively, you can open your preferred IDE, press the "create new project" button, and find the template.

4. Create a new project with this template either from the IDE or using the following console command:

    ```console
    dotnet new CleanWebApiTemplate -o {NameOfYourProject}
    ```

    **NOTE:** It's important that you close the CleanWebApiTemplate from your IDE, otherwise there can be problems creating the project

5. You want to test the created WebApi against a database in DEVELOPMENT mode? Don't forget to create a container DB:

    - If you selected SQL Server:

    ```console
    docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=yourStrong(!)Password" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:latest
    ```

    - If you selected MongoDB
    ```console
    docker run -d -p 27017:27017 --name mongodb -e MONGO_INITDB_ROOT_USERNAME=admin -e MONGO_INITDB_ROOT_PASSWORD=adminpass mongo:latest
    ```

    Right click on Host -> Manage User Secrets

    Paste configuration file content found inside the my_user_secrets.txt file

    
## Uninstalling the template

In a console, anywhere, execute this command:
```console
dotnet new uninstall CleanWebApiTemplate
```

## Applying Initial Migrations to the Database

When a new Web API Project has been created, to apply the initial migrations to the database you will need to use the next following commands:

```console
$env:ASPNETCORE_ENVIRONMENT = "Development" 
```
```console
dotnet ef migrations add InitialMigration --project {Project where context is allocated} --startup-project {Startup project} -o {Output dir FROM THE PROJECT, in our case, Migrations} 
```
```console
dotnet ef database update --project {Project where context is allocated} --startup-project {Startup project} 
```

### Example:

```console
$env:ASPNETCORE_ENVIRONMENT = "Development" 
```
```console
dotnet ef migrations add InitialMigration --project .\src\CleanWebApiTemplate.Infrastructure\CleanWebApiTemplate.Infrastructure.csproj --startup-project .\src\CleanWebApiTemplate.Host\ -o Migrations
```
```console
dotnet ef database update --project .\src\CleanWebApiTemplate.Infrastructure\CleanWebApiTemplate.Infrastructure.csproj --startup-project .\src\CleanWebApiTemplate.Host\ 
```