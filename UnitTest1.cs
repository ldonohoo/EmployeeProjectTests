

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TheEmployeeAPI.Employees;

namespace TheEmployeeAPI.Tests;
public class BasicTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly int _employeeId = 1;
    private readonly WebApplicationFactory<Program> _factory;

    public BasicTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllEmployees_ReturnsOkResult()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/employees");

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to get employees: {content}");
        }

        var employees = await response.Content.ReadFromJsonAsync<IEnumerable<GetEmployeeResponse>>();
        Assert.NotEmpty(employees);
    }

    [Fact]
    public async Task GetAllEmployees_WithFilter_ReturnsOneResult()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/employees?FirstNameContains=John");

        response.EnsureSuccessStatusCode();

        var employees = await response.Content.ReadFromJsonAsync<IEnumerable<GetEmployeeResponse>>();
        Assert.Single(employees);
    }

    [Fact]
    public async Task GetEmployeeById_ReturnsOkResult()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/employees/1");

        response.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task CreateEmployee_ReturnsCreatedResult()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/employees",
            new Employee { FirstName = "Toki", LastName = "theDog", SocialSecurityNumber = "123-45-0001" });
        response.EnsureSuccessStatusCode();
    }

    // sends a bad employee object (empty) to see what happens
    // should 
    [Fact]
    public async Task CreateEmployee_ReturnsBadRequestResult()
    {
        // arrange (create the client and the invalid employee)
        var client = _factory.CreateClient();
        var invalidEmployee = new CreateEmployeeRequest(); //empty object

        // act (post the empty invalidEmployee)
        var response = await client.PostAsJsonAsync("/employees", invalidEmployee);

        // assert
        // first: checks the StatusCode in response to see if it is a 
        //           bad request status code
        //           HttpStatusCode is an enum of status codes!! 400 = bad request
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // check the problem details for a variety of things
        //   - in the real world you won't test validation for
        //     every property on every request, 
        //   - INSTEAD, find the most important properties/behavoirs to
        //     validate and focus on those first!
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        // so when we pass an empty employee object up:
        //  - the problem details wont be null
        //  - we will have error keys for firstname & lastname
        //  - we will have errors about required firstname & lastname
        Assert.NotNull(problemDetails);
        Assert.Contains("FirstName", problemDetails.Errors.Keys);
        Assert.Contains("LastName", problemDetails.Errors.Keys);
        Assert.Contains("'First Name' must not be empty.", problemDetails.Errors["FirstName"]);
        Assert.Contains("'Last Name' must not be empty.", problemDetails.Errors["LastName"]);
    }
        [Fact]
    public async Task UpdateEmployee_ReturnsSuccessful()
    {
        var client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync("/employees/1",
            new UpdateEmployeeRequest {
                Address1 = "123 dale st",
                Address2 = null,
                City =  null,
                State = null,
                ZipCode =  null,
                PhoneNumber =  null,
                Email = null
        });
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task UpdateEmployee_ReturnsNotFoundForNonExistantEmployee()
    {
        var client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync(
            "/employees/99",
            new UpdateEmployeeRequest {
                Address1 = "123 dale st",
                Address2 = null,
                City =  null,
                State = null,
                ZipCode =  null,
                PhoneNumber =  null,
                Email = null
        });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        // // first create the web client to run test
        // var client = _factory.CreateClient();

        // // then, use the client to send a put request
        // var response = await client.PutAsJsonAsync("/employees/99",
        //     new Employee {
        //             FirstName = "Jane",
        //             LastName = "Doe",
        //             SocialSecurityNumber = "123",
        //             Address1 = "123 dynamo drive    "
        //         }              
        // );
        
        // using var scope = _factory.Services.CreateScope();
        // var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // var employee = await db.Employees.FindAsync(99);
        // // then check response to see that it is 
        // Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEmployee_ReturnsOkResult()
    {
        var client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync("/employees/1",
            new UpdateEmployeeRequest {
                Address1 = "123 Main Smoot",
                Address2 = null,
                City =  null,
                State = null,
                ZipCode =  null,
                PhoneNumber =  null,
                Email = null
        });

        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var employee = await db.Employees.FindAsync(1);
        Assert.Equal("123 Main Smoot", employee.Address1);
    }

    [Fact]
    public async Task UpdateEmployee_ReturnsBadRequestWhenAddress()
    {
        // Arrange
        var client = _factory.CreateClient();
        var invalidEmployee = new UpdateEmployeeRequest(); // Empty object to trigger validation errors

        // Act
        var response = await client.PutAsJsonAsync($"/employees/{_employeeId}", invalidEmployee);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Contains("Address1", problemDetails.Errors.Keys);
    }

    [Fact]
    public async Task GetBenefitsForEmployee_ReturnsOkResult()
    {
        // Act
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/employees/{_employeeId}/benefits");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var benefits = await response.Content.ReadFromJsonAsync<IEnumerable<GetEmployeeResponseEmployeeBenefit>>();
        Assert.Equal(2, benefits.Count());
    }

    [Fact]
    public async Task DeleteEmployee_ReturnsNoContentResult()
    {
        var client = _factory.CreateClient();

        var newEmployee = new Employee { FirstName = "Meow", LastName = "Garita" };
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Employees.Add(newEmployee);
            await db.SaveChangesAsync();
        }

        var response = await client.DeleteAsync($"/employees/{newEmployee.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEmployee_ReturnsNotFoundResult()
    {
        var client = _factory.CreateClient();
        var response = await client.DeleteAsync("/employees/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

 }