# SleepEditWeb Koyeb Deployment Context

## Project Overview
- **Application**: SleepEditWeb - ASP.NET Core 8.0 MVC web application
- **Primary Feature**: Medication list management with autocomplete functionality
- **Deployment Platform**: Koyeb (PaaS)
- **Repository**: github.com/d-german/SleepEditWeb
- **Live URL**: https://unfair-cordelia-d-german-6a81de88.koyeb.app/

## Critical Issue Resolved
The main problem was that the `medlist.txt` file (740KB, 31,229 medications) was not being found at runtime in the Docker container deployed to Koyeb.

### Root Cause
The `Resources/medlist.txt` file was not being included in the Docker image because:
1. The .csproj file needed `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>` for the Resources folder
2. The Dockerfile needed explicit COPY instructions to ensure the file was in the final runtime image

### Solution Implemented
1. **Updated SleepEditWeb.csproj** to include:
   ```xml
   <ItemGroup>
     <Content Include="Resources\**\*">
       <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
       <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
     </Content>
   </ItemGroup>
   ```

2. **Updated Dockerfile** with explicit copy and verification steps:
   - Multi-stage build (sdk:8.0 for build, aspnet:8.0 for runtime)
   - Copy Resources folder during build
   - Verify presence in publish output
   - Echo checks at each stage to confirm file presence

3. **Latest Working Dockerfile** (commit 005e98a):
   - Structured to invalidate cache and force fresh builds
   - Verification steps showing medlist.txt is present with 31,229 lines
   - Final image confirms file at `/app/Resources/medlist.txt`

### Build Verification
Latest successful build logs show:
```
=== Checking source ===
-rw-r--r-- 1 root root 740027 Dec 19 04:30 medlist.txt
31229 /source/SleepEditWeb/Resources/medlist.txt

=== Checking publish ===
-rw-r--r-- 1 root root 740027 Dec 19 04:30 medlist.txt
31229 /publish/Resources/medlist.txt

=== Final check ===
-rw-r--r-- 1 root root 740027 Dec 19 04:30 medlist.txt
```

## Current Application Behavior
- **Medication Loading**: Successfully loads 31,229 medications from embedded resource
- **Add Functionality**: Users can add new medications (stored in file system)
- **Remove Functionality**: Users can remove medications they added (file system only)
- **Embedded Resource Protection**: Medications from the original medlist.txt cannot be removed by users
- **Session Management**: Uses cookies with DataProtection (warnings in logs are normal for containerized apps)

## Key Files
1. **SleepEditWeb/Resources/medlist.txt** - 740KB medication database
2. **SleepEditWeb/Controllers/MedListController.cs** - Handles medication CRUD operations
3. **Dockerfile** - Multi-stage Docker build configuration
4. **SleepEditWeb.csproj** - Project configuration with Content includes

## Diagnostic Endpoints
- `/MedList/DiagnosticInfo` - Shows file path, existence, med count, and directory contents
- Previous diagnostic showed `fileExists: false` before fix
- Now should show successful loading

## Known Issues & Warnings (Non-Critical)
1. **DataProtection Keys**: Warnings about storing keys in `/root/.aspnet/DataProtection-Keys` - normal for containerized apps without persistent storage
2. **Session Cookie Unprotect Errors**: Occur when container restarts and old cookies reference old keys - harmless
3. **HTTPS Redirect Warning**: Expected in Koyeb environment which handles TLS at proxy level
4. **App Name**: Koyeb auto-generates names like "unfair-cordelia" - user wants to investigate changing this

## Koyeb Deployment Notes
- **Build Process**: Koyeb clones repo, builds Docker image, pushes to internal registry
- **Health Checks**: Application must respond on port 8000 (configured in Koyeb settings)
- **Environment**: Production mode, listening on `http://[::]:8000`
- **Content Root**: `/app`

## Recent Commits (Chronological)
1. `61b5ffb` - Remove Azure and unused workflows - Koyeb deploys directly from GitHub
2. `05cb735` - Dockerfile: Add fallback explicit copy of Resources folder
3. `005e98a` - Restructure Dockerfile to force complete cache invalidation (CURRENT - WORKING)

## How to Delete and Recreate Koyeb App (For Better Naming)

### Why Delete and Recreate?
Koyeb auto-generated the app name "unfair-cordelia-d-german-6a81de88" which appears in the URL. Unfortunately, **Koyeb does not allow renaming existing apps** - the only way to get a better name is to delete and recreate the app.

### Pre-Deletion Checklist
Before deleting the app, ensure:
1. ✅ All code is committed and pushed to GitHub
2. ✅ Dockerfile is in the repository root
3. ✅ Repository settings are correct on GitHub
4. ✅ You have admin access to the Koyeb account
5. ✅ You understand the current configuration (see below)

### Current Koyeb Configuration (To Replicate)
- **Service Name**: SleepEditWeb (or similar)
- **Deployment Method**: GitHub repository
- **Repository**: d-german/SleepEditWeb
- **Branch**: main (or default branch)
- **Build Method**: Dockerfile
- **Dockerfile Path**: `Dockerfile` (in root)
- **Port**: 8000
- **Region**: Choose closest to users (e.g., Washington DC, Frankfurt, Singapore)
- **Instance Type**: Free tier or Nano
- **Auto-deploy**: Enabled (deploys on git push)

### Step-by-Step Deletion Process

#### 1. Access Koyeb Dashboard
   - Navigate to: https://app.koyeb.com/
   - Log in with your credentials

#### 2. Locate Your Service
   - Click on "Services" in the left sidebar
   - Find "SleepEditWeb" or your current service name
   - Current URL: https://unfair-cordelia-d-german-6a81de88.koyeb.app/

#### 3. Delete the Service
   - Click on the service name to open details
   - Click "Settings" tab (top right area)
   - Scroll to bottom: Find "Delete Service" button
   - Click "Delete Service"
   - **Important**: Confirm deletion - type the service name if prompted
   - Wait for deletion to complete (~30 seconds)

### Step-by-Step Recreation Process

#### 1. Create New Service
   - On Koyeb dashboard, click "Create Service" button
   - Select "GitHub" as the deployment source

#### 2. Connect GitHub (if not already connected)
   - Authorize Koyeb to access your GitHub account
   - Select the repository: `d-german/SleepEditWeb`
   - Select branch: `main` (or your default branch)

#### 3. Configure Build Settings
   - **Builder**: Select "Dockerfile"
   - **Dockerfile path**: Leave as `Dockerfile` (default, looks in root)
   - **Build context**: Leave as root `/`

#### 4. Configure Deployment Settings
   - **Service Name**: Choose your preferred name (e.g., "sleepedit", "medlist-app", "medication-search")
     - This becomes part of URL: `https://YOUR-NAME-xxxxx.koyeb.app/`
     - Use lowercase, hyphens allowed
     - Keep it short and professional
   - **Region**: Select preferred region (e.g., `was` for Washington DC)
   - **Instance Type**: 
     - Free: 512MB RAM, shared CPU (sufficient for this app)
     - Nano: $5/month, dedicated resources

#### 5. Configure Port and Health Check
   - **Port**: Set to `8000` (critical - must match Program.cs)
   - **Protocol**: HTTP
   - **Health Check Path**: `/` (root page)
   - **Health Check Port**: `8000`

#### 6. Environment Variables (if any)
   - For this app, none are required
   - ASPNETCORE_ENVIRONMENT is automatically set to "Production"

#### 7. Auto-Deploy Settings
   - **Enable Auto-Deploy**: Check this box
   - **Branch**: Select `main` (or your default)
   - This enables automatic redeployment on git push

#### 8. Review and Deploy
   - Review all settings
   - Click "Deploy" button
   - Wait for initial build (~2-5 minutes)

#### 9. Monitor Deployment
   - Watch build logs in real-time
   - Verify the build stages complete:
     - Cloning repository
     - Building Docker image (multi-stage build)
     - Pushing to Koyeb registry
     - Starting container
     - Health checks passing
   - Status should change to "Healthy" when ready

#### 10. Test New Deployment
   - Note your new URL: `https://YOUR-NAME-xxxxx.koyeb.app/`
   - Test homepage: Should load MVC site
   - Test medication search: Type a few letters, autocomplete should work
   - Test MedList page: `/MedList`
   - Test diagnostic endpoint: `/MedList/DiagnosticInfo` (should show medlist found)

### Expected Build Time
- Initial build: 2-5 minutes
- Subsequent builds: 1-3 minutes (with caching)

### Troubleshooting New Deployment

If build fails:
1. Check build logs for errors
2. Verify Dockerfile is in repository root
3. Ensure latest code is pushed to GitHub
4. Check that port 8000 is configured correctly

If app doesn't start:
1. Check runtime logs for errors
2. Verify health check endpoint is responding
3. Check that Resources/medlist.txt is present (diagnostic endpoint)
4. Review DataProtection warnings (harmless) vs actual errors

If medications don't load:
1. Access `/MedList/DiagnosticInfo`
2. Verify `fileExists: true` and `medListCount: 31229`
3. If false, check .csproj has Content Include for Resources
4. Verify Dockerfile copies Resources folder correctly

### Important Notes
- **URL Structure**: Koyeb adds random suffix to prevent collisions (e.g., `your-name-a1b2c3d4.koyeb.app`)
- **No Downtime Required**: You can keep old service running until new one is verified
- **Custom Domains**: Koyeb supports custom domains if you want to use your own (e.g., `medlist.yourdomain.com`)
- **SSL/TLS**: Automatically provided by Koyeb for all `*.koyeb.app` domains
- **Logs**: Available in Koyeb dashboard under "Logs" tab

### Cost Considerations
- **Free Tier**: Limited to 2 services, 512MB RAM, shared CPU
- **Paid Tier**: Starts at $5/month for dedicated nano instance
- **No hidden fees**: Bandwidth and requests included

### Post-Recreation Steps
1. Update any bookmarks with new URL
2. Update documentation/README if URL is documented
3. Test all functionality thoroughly
4. Monitor first few deployments to ensure auto-deploy works
5. Consider adding custom domain if this is production

### Alternative: Custom Domain
Instead of recreating for a better name, you can:
1. Keep current deployment
2. Add a custom domain (e.g., `meds.example.com`)
3. Configure DNS CNAME record pointing to Koyeb
4. Use custom domain as primary URL

This avoids recreation but requires domain ownership.

## Next Steps / User Questions