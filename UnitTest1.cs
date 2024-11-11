

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TheEmployeeAPI.Abstractions;
using TheEmployeeAPI.Employees;

namespace TheEmployeeAPI.Tests;

public class BasicTests: IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    // this is our test constructor
    public BasicTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        // add initializing our repo with two employees
        // first we get the required service for the repo with the
        //  GetRequiredService method.  Then we create the repo with
        // two initial employees...
        var repo = _factory.Services.GetRequiredService<IRepository<Employee>>();
        repo.Create(new Employee { FirstName = "John", LastName = "Doe" });
    }

    // here we write a test for our first GetAllEmployees endpoint
    //
    // create with type Task as this is a operation to be completed
    // in the future (like a Promise)
    //
    // fact tells the system this is a test!! needed!!
    // these fact methods each represent a single test!
    [Fact]
    public async Task GetAllEmployees_ReturnsOkResult()
    {
        // clientfactory can create a fake client to test our API!!!
        // here we use the WebApplicationFactory instance method
        // to create a new client so we can use to test the 
        //  backend!!
        HttpClient client = _factory.CreateClient();
        var response = await client.GetAsync("/employees");

        // can do it this way too using assert library in xunit..
        // Assert.True(response.IsSuccessStatusCode);
        // 
        // but this will look at response and throw an error if
        //  a non-200 status code is received!
        response.EnsureSuccessStatusCode();

    }

    [Fact]
    public async Task GetEmployeesById_ReturnsOkResult()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/employees/1");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetEmployeesById_ReturnsNotFoundResult()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/employees/555555");

    // asserts are always in order expectedValue, actualValue!!!!
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
            new Employee {
            FirstName =  "Jane",
            LastName = "DoeRAEmeFAsoLAteaDoh",
            SocialSecurityNumber = "123-45-3446",
            Address1 = null,
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
        // first create the web client to run test
        var client = _factory.CreateClient();

        // then, use the client to send a put request
        var response = await client.PutAsJsonAsync("/employees/99999",
            new Employee {
            FirstName =  "Jane",
            LastName = "DoeRAEmeFAsoLAteaDoh",
            SocialSecurityNumber = "123-45-3446",
        });

        // then check response to see that it is 
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

    }
}