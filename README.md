# Capturing and Inspecting HTTPS Traffic with Fiddler Classic

## Overview

This tutorial demonstrates how to capture and inspect HTTPS traffic from a .NET 10 Minimal API using Fiddler Classic. You'll learn to monitor both incoming requests from a client (Bruno API Client) and outgoing requests from your API to external services.

## Prerequisites

- .NET 10 SDK installed
- Fiddler Classic (free) - [Download here](https://www.telerik.com/fiddler/fiddler-classic)
- Bruno API Client (or any API client) - [Download here](https://www.usebruno.com/)
- Windows OS (for Fiddler Classic)

## Architecture

```
Bruno API Client → Local .NET API → External API
                      ↓
                  Fiddler Classic captures both requests
```

## Step 1: Configure Fiddler Classic

### 1.1 Install and Launch Fiddler Classic

Download and install Fiddler Classic from the official website.

### 1.2 Enable HTTPS Decryption

1. Open **Fiddler Classic**
2. Navigate to **Tools → Options → HTTPS** tab
3. Enable the following options:
   - ✅ **Capture HTTPS CONNECTs**
   - ✅ **Decrypt HTTPS traffic**
   - Select ```...from all processes``` in the dropdown

### 1.3 Trust Fiddler's Root Certificate

1. In the same **HTTPS** tab, click **Actions** button
2. Select **Trust Root Certificate**
3. Click **Yes** when Windows prompts to install the certificate
4. Click **OK** to close the Options dialog

### 1.4 Verify Proxy Settings

1. Go to **Tools → Options → Connections** tab
2. Note the port number (default: **8888**)
3. Ensure **Allow remote computers to connect** is checked (optional, for remote debugging)

## Step 2: Create the .NET 10 Minimal API

### 2.1 Create a New Project

```bash
# Create a new directory
mkdir TrafficCaptureDemo
cd TrafficCaptureDemo.Api

# Create a new .NET 10 Minimal API project
dotnet new web -n TrafficCaptureDemo.Api
cd TrafficCaptureDemo.Api
```

### 2.2 Implement the API

Add Swashbuckle.AspNetCore package for Swagger support:
```bash
dotnet add package Swashbuckle.AspNetCore
``` 

Replace the contents of `Program.cs` with the following code:

```csharp
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger UI in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Root endpoint - simple health check
app.MapGet("/", () => Results.Ok(new
{
    Message = "Traffic Capture Demo API",
    Timestamp = DateTime.UtcNow
}));

// Endpoint that calls an external API
app.MapGet("/api/user/{id}", async (int id, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();

    try
    {
        var userResponse = await client.GetStringAsync(
            $"https://jsonplaceholder.typicode.com/users/{id}");

        var user = JsonSerializer.Deserialize<JsonElement>(userResponse);

        var postsResponse = await client.GetStringAsync(
            $"https://jsonplaceholder.typicode.com/posts?userId={id}");

        var posts = JsonSerializer.Deserialize<JsonElement>(postsResponse);

        return Results.Ok(new
        {
            Message = "Data retrieved successfully",
            User = user,
            Posts = posts,
            Timestamp = DateTime.UtcNow
        });
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem($"Error calling external API: {ex.Message}");
    }
});

// Endpoint that demonstrates POST to external API
app.MapPost("/api/create-post", async (CreatePostRequest request, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();

    try
    {
        var postData = new
        {
            title = request.Title,
            body = request.Body,
            userId = request.UserId
        };

        var jsonContent = JsonContent.Create(postData);

        var response = await client.PostAsync(
            "https://jsonplaceholder.typicode.com/posts",
            jsonContent);

        var responseContent = await response.Content.ReadAsStringAsync();

        return Results.Ok(new
        {
            Message = "Post created successfully",
            StatusCode = (int)response.StatusCode,
            Response = JsonSerializer.Deserialize<JsonElement>(responseContent)
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error: {ex.Message}");
    }
});

// Let Kestrel use configured URLs (from launchSettings.json / environment)
app.Run();

// Request model for POST endpoint
record CreatePostRequest(string Title, string Body, int UserId);
```

Replace the contents of ```Properties/launchSettings.json``` with the following code:
```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5268",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:5001;http://localhost:5268",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}

```

### 2.3 Run the API

```bash
dotnet run
```

Your API should now be running at `https://localhost:5001`

## Step 3: Configure Bruno API Client

### 3.1 Create a New Collection

1. Open **Bruno**
2. Create a new collection named "Traffic Capture Demo"

### 3.2 Configure Proxy Settings

1. Click on the **collection settings** (gear icon or three dots menu)
2. Go to **Proxy** settings
3. Configure as follows:
   - **Config**: enabled
   - **Protocol**: HTTP
   - **Hostname**: `127.0.0.1`
   - **Port**: `8888`
4. Enable the proxy
5. Save the settings

### 3.3 Disable SSL Verification (Development Only)

1. In the request settings or collection settings
2. Find **SSL Certificate Verification**
3. Disable it (this allows Bruno to accept Fiddler's certificate)

## Step 4: Create Test Requests in Bruno

### Request 1: Simple GET

```
GET https://localhost:5001/
```

### Request 2: GET with External API Call

```
GET https://localhost:5001/api/user/1
```

### Request 3: POST with External API Call

```
POST https://localhost:5001/api/create-post
Content-Type: application/json

{
  "title": "Test Post from Bruno",
  "body": "This is a test post captured by Fiddler",
  "userId": 1
}
```

## Step 5: Capture and Inspect Traffic

### 5.1 Start Capturing

1. Ensure **Fiddler Classic** is running
2. Click **F12** or the **Capturing** button to ensure capture is active (status bar shows "Capturing")

### 5.2 Make Requests

1. In **Bruno**, send the GET request to `/api/user/1`
2. Switch to **Fiddler Classic**

### 5.3 Inspect the Traffic

You should see **three requests** in Fiddler's session list:

#### Request 1: Bruno → Your Local API
- **Host**: `localhost:5001`
- **URL**: `/api/user/1`
- **Protocol**: HTTPS (with a lock icon)

**To inspect:**
1. Click on this session
2. Go to **Inspectors** tab
3. **Request** section:
   - View headers (User-Agent, Accept, etc.)
   - View raw request
4. **Response** section:
   - View JSON response
   - View headers

#### Request 2: Your API → External API (User Data)
- **Host**: `jsonplaceholder.typicode.com`
- **URL**: `/users/1`
- **Protocol**: HTTPS

#### Request 3: Your API → External API (Posts)
- **Host**: `jsonplaceholder.typicode.com`
- **URL**: `/posts?userId=1`
- **Protocol**: HTTPS

### 5.4 Verify HTTPS Decryption

For each HTTPS request, verify that:
- The **lock icon** appears (indicating HTTPS)
- You can see the **full request and response bodies** in plain text
- Headers are fully visible
- JSON payloads are readable

**If you see "Tunnel to..." instead**: HTTPS decryption is not working. Go back to Step 1.2 and ensure the settings are correct.

## Step 6: Advanced Inspection Techniques

### 6.1 Use Filters

To focus on your API traffic only:
1. Go to **Filters** tab in Fiddler
2. Under **Hosts**, select "Show only the following Hosts"
3. Add: `localhost:5001; jsonplaceholder.typicode.com`
4. Click **Actions → Run Filterset now**

### 6.2 Compare Requests

1. Select multiple sessions (Ctrl+Click)
2. Right-click → **Compare** → **In WinDiff**
3. Compare request/response payloads

### 6.3 Save Sessions

1. Select sessions you want to save
2. **File → Save → Selected Sessions**
3. Save as `.saz` file (Fiddler's session archive format)
4. Share with team members or review later

### 6.4 AutoResponder (Mock Responses)

Test your API's error handling:
1. Go to **AutoResponder** tab
2. Enable **Enable Rules** and **Unmatched requests passthrough**
3. Add a rule:
   - **Rule Editor**: `EXACT:https://jsonplaceholder.typicode.com/users/1`
   - **Response**: `*404` or create custom response
4. Make the request again - your API will receive the mocked response

## Step 7: Clean Up

### 7.1 When Finished Testing

1. **Stop Fiddler**: Close Fiddler Classic
2. **Disable proxy in Bruno**: Go back to collection settings and disable the proxy
3. **Clear Fiddler sessions**: In Fiddler, press Ctrl+X to remove all sessions

### 7.2 Certificate Management (Optional)

If you want to remove Fiddler's certificate:
1. Press `Win + R`
2. Type: `certmgr.msc`
3. Navigate to: **Trusted Root Certification Authorities → Certificates**
4. Find **DO_NOT_TRUST_FiddlerRoot**
5. Right-click → **Delete**

**Note**: Only remove this in non-development environments or when completely done with Fiddler.

## Troubleshooting

### Bruno Requests Don't Appear in Fiddler

**Problem**: Requests from Bruno are not captured.

**Solutions**:
- Verify proxy configuration in Bruno: `127.0.0.1:8888` with HTTP protocol
- Check Fiddler is running and capturing (F12 key)
- Ensure "Capturing" status is active in Fiddler's status bar

### External API Requests Don't Appear

**Problem**: Only the Bruno → API request appears, not API → External API.

**Solutions**:
- Verify your .NET API is using default `HttpClient` (respects system proxy)
- Check Windows system proxy settings (Fiddler should set these automatically)
- Restart your .NET API after starting Fiddler

### SSL/TLS Errors in Bruno

**Problem**: Bruno shows SSL certificate errors.

**Solutions**:
- Disable SSL certificate verification in Bruno (development only)
- Verify Fiddler's certificate is installed and trusted (Step 1.3)

### "Unable to Decrypt HTTPS Traffic" Warning

**Problem**: Fiddler shows this warning for some sessions.

**Solutions**:
- Ensure "Decrypt HTTPS traffic" is enabled in Fiddler
- Reinstall Fiddler's root certificate: Tools → Options → HTTPS → Actions → Trust Root Certificate
- Restart Fiddler after certificate installation

### API Returns Proxy Errors

**Problem**: Your API returns 502 or proxy-related errors.

**Solutions**:
- Ensure Fiddler is running before making requests
- Check Fiddler's proxy port is 8888 (or update Bruno accordingly)
- Verify no other applications are using port 8888

## Security Considerations

⚠️ **Important Security Notes**:

1. **Development Only**: The Fiddler certificate (`DO_NOT_TRUST_FiddlerRoot`) should only be installed on development machines
2. **Never in Production**: Never deploy applications with hardcoded proxy settings or disabled SSL verification
3. **Certificate Trust**: The Fiddler certificate creates a security vulnerability if left installed on production systems
4. **Sensitive Data**: Be cautious when capturing traffic containing passwords, API keys, or personal information
5. **Share Carefully**: `.saz` session files may contain sensitive data - review before sharing

## Additional Resources

- [Fiddler Classic Documentation](https://docs.telerik.com/fiddler/configure-fiddler/tasks/configurefiddler)
- [.NET HttpClient Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient)
- [Bruno API Client Documentation](https://docs.usebruno.com/)
- [JSONPlaceholder - Free API for Testing](https://jsonplaceholder.typicode.com/)

## Summary

You've successfully:
- ✅ Configured Fiddler Classic to decrypt HTTPS traffic
- ✅ Created a .NET 10 Minimal API that calls external services
- ✅ Configured Bruno to route traffic through Fiddler
- ✅ Captured and inspected both incoming and outgoing HTTPS requests
- ✅ Verified all request/response payloads are visible despite encryption

This setup is invaluable for debugging API integrations, understanding traffic flow, and troubleshooting issues in development environments.

## Updates: Program.cs and launchSettings.json (F5 / Swagger fixes)

This project was updated to avoid a port mismatch that could prevent the browser from opening when running the app with F5 in Visual Studio. The notes below explain what changed and how to verify the fix.

### What changed

- `Program.cs`
  - Removed the hard-coded call `app.Run("https://localhost:5001")`.
  - Now the app calls `app.Run()` so Kestrel uses the URLs provided by `launchSettings.json` or environment variables.
  - Swagger UI is enabled in Development with `app.UseSwagger()` and `app.UseSwaggerUI()` so `launchUrl: "swagger"` opens the Swagger UI when the IDE launches the browser.

- `Properties/launchSettings.json`
  - Profiles contain `applicationUrl` entries used by Visual Studio when launching the app (example values in this repository):
    - `http` profile: `http://localhost:5268`
    - `https` profile: `https://localhost:7103;http://localhost:5268`
  - Both profiles have `launchBrowser: true` and `launchUrl: "swagger"` so Visual Studio will open the Swagger UI when starting the app from the IDE.

### How to verify in Visual Studio

1. Select the desired launch profile from the debug dropdown (the profile names defined in `launchSettings.json`).
2. Ensure the project is set as the startup project: right-click the project → **Set as Startup Project**.
3. Start debugging with **Start Debugging** (F5).
4. Open the **Output** window and check for the Kestrel binding lines (look for `Now listening on:`). The URLs listed there are the addresses the app is bound to and the browser should open to the `launchUrl` (for example, `/swagger`).

### Troubleshooting if the browser still doesn't open

- Confirm the active launch profile has `launchBrowser: true` and `launchUrl` set.
- Make sure no other process is occupying the configured ports.
- If the browser opens but shows 404 for `/swagger`, confirm `app.UseSwagger()` and `app.UseSwaggerUI()` are invoked when `app.Environment.IsDevelopment()` is true.
- Verify your system's default browser is not blocking the automatic open.
