# Domain Name & Koyeb Deployment Guide for SleepEditWeb

**Date**: December 19, 2025  
**Project**: SleepEditWeb - Medical Medication List Application  
**Current Deployment**: Koyeb (free tier)

---

## Current Koyeb Setup

### Application Details
- **Service Name**: sleep-edit
- **Current URL**: `sleep-edit.defiant-ethel-[hash].koyeb.app`
- **Deployment**: Git-driven from GitHub repo `d-german/SleepEditWeb`
- **Docker**: Uses Dockerfile in repo root
- **Health Status**: ✅ Working (medications loading from embedded resource)

### Persistent Storage Status
- **Volume**: ⚠️ NOT YET CONFIGURED
- **Code Updated**: ✅ MedListController now uses `/app/Data/medlist.txt`
- **Action Needed**: Add volume in Koyeb dashboard

### Adding Volume to Existing Koyeb Service
1. Go to Koyeb dashboard → Your service
2. Click "Settings" → "Edit Service"
3. Scroll to "Volumes" or "Persistent Storage"
4. Click "Add Volume":
   - **Mount path**: `/app/Data`
   - **Size**: 1 GB (sufficient for text files)
5. Save → Koyeb will redeploy automatically
6. After redeploy, user-added medications (using `+medication`) will persist across restarts

---

## Domain Name Recommendations

### Best Options for SleepEditWeb + Future Blog

#### 🏆 Recommended: `.net`
- **Cost**: $10-13/year
- **Best for**: Interactive apps AND content/blogs (versatile)
- **Trust level**: Very high (established since 1985)
- **Connotation**: Neutral, professional, no limitations
- **Example**: `yourname.net` or `sleepedit.net`
- **Why**: Perfect for both the medical app and blog content, affordable, trusted by medical professionals

#### Alternative: `.app`
- **Cost**: $15-18/year
- **Best for**: Any web application (interactive or content-based)
- **Trust level**: High (Google-owned, requires HTTPS)
- **Connotation**: "This is an application"
- **Example**: `sleepedit.app`
- **Why**: Modern, professional, works for both use cases but slightly more expensive

#### Alternative: `.com`
- **Cost**: $10-15/year
- **Best for**: Maximum traditional trust
- **Trust level**: Highest (universally recognized)
- **Connotation**: Commercial/established
- **Example**: `sleepedit.com`
- **Why**: Most familiar to all audiences

### ❌ Avoid These:

- **`.dev`**: Too technical, might imply "work in progress" to non-tech users
- **`.xyz`**: Spam reputation, cheap perception, might hurt trust for medical app
- **`.io`**: Too expensive ($30-40/year), startup-focused

---

## Subdomain Strategy (One Domain, Multiple Apps)

When you buy one domain (e.g., `yourname.net`), you get unlimited FREE subdomains:

```
Main domain:     yourname.net
├── sleep.yourname.net         → Koyeb SleepEditWeb app
├── blog.yourname.net          → Blog/reading platform
├── portfolio.yourname.net     → Portfolio site
├── test.yourname.net          → Test environment
└── www.yourname.net          → Main landing page
```

**Cost**: $10-15/year total (subdomains are free DNS records)

---

## Cheapest Domain Registrar Options (2025)

### 🏆 Cloudflare Registrar (At-Cost Pricing)
- **Cost**: Registry wholesale price (no markup)
  - `.com`: ~$9-10/year
  - `.net`: ~$10-11/year
  - `.app`: ~$15/year
- **Features**:
  - ✅ Free WHOIS privacy
  - ✅ Free DNSSEC
  - ✅ Auto-renewal at list price (no surprise increases)
  - ✅ Best DNS management (same dashboard as hosting)
- **Requirement**: Must use Cloudflare DNS (free, easy to set up)
- **Setup**: Intermediate (requires Cloudflare account and DNS setup first)
- **Best for**: Best overall value if you're comfortable with DNS setup

### Porkbun
- **Cost**: 
  - `.com`: ~$9-11/year
  - `.net`: ~$11-13/year
  - `.app`: ~$16-18/year
- **Features**:
  - ✅ Free WHOIS privacy
  - ✅ Free SSL certificates
  - ✅ Simple DNS management
- **Setup**: Beginner-friendly
- **Best for**: Easiest setup with competitive pricing

### Namecheap
- **Cost**:
  - `.com`: ~$10-13/year (first year often $8-9 with promo)
  - `.net`: ~$12-14/year
  - `.app`: ~$17-20/year
- **Features**:
  - ✅ Free WHOIS privacy (first year)
  - ✅ Simple interface
  - ✅ Good support
- **Setup**: Beginner-friendly
- **Best for**: Easiest for beginners, established reputation

---

## Connecting Custom Domain to Koyeb

### Prerequisites
1. Domain purchased from registrar
2. Access to domain's DNS settings

### Setup Steps

#### In Koyeb Dashboard:
1. Go to your service → Settings
2. Find "Domains" or "Custom Domains" section
3. Click "Add Domain"
4. Enter your domain (e.g., `sleep.yourname.net`)
5. Koyeb will show you DNS records to add

#### At Your Domain Registrar:
Add the DNS records Koyeb provides (typically):
```
Type: CNAME
Name: sleep (or your subdomain)
Value: [your-app].koyeb.app
TTL: Auto or 3600
```

#### Wait for DNS Propagation:
- Usually: 5-30 minutes
- Sometimes: Up to 48 hours
- Check status in Koyeb dashboard

---

## Recommended Action Plan

### Step 1: Add Volume to Current Koyeb Service ⚡ DO THIS FIRST
- Koyeb dashboard → Edit Service → Add Volume (`/app/Data`, 1GB)
- This enables persistent medication additions

### Step 2: Test Current Deployment
- Verify medications load correctly
- Test adding medication with `+testmedicine`
- Test removal with `-testmedicine`
- Check persistence after Koyeb redeploy

### Step 3: Domain Purchase (Optional)
- **Recommended**: Buy `yourname.net` or `sleepedit.net`
- **Where**: Cloudflare (cheapest), Porkbun (easiest), or Namecheap (beginner-friendly)
- **Cost**: $10-15/year

### Step 4: Connect Domain to Koyeb
- Add custom domain in Koyeb
- Configure DNS records at registrar
- Wait for propagation
- Test: `sleep.yourname.net`

---

## Koyeb Service Management

### Creating Multiple Services (Different URLs)
- You CAN create multiple services to get different auto-generated URLs
- Each service gets: `[service-name].[random-adjective-name]-[hash].koyeb.app`
- Keep the one you like, delete others
- Free tier allows this experimentation

### Deleting a Koyeb Service
1. Koyeb dashboard → Services
2. Click on service to delete
3. Settings → "Delete Service"
4. Confirm deletion
5. Service and its URL are removed immediately

### Starting Fresh with Better Name
1. Delete current service (if desired)
2. Create new service with better name
3. Same GitHub repo, same settings
4. Get new auto-generated URL
5. Repeat until you like the URL OR just buy custom domain

---

## File Structure Context

### Medication List Files
- **Embedded Resource**: `SleepEditWeb/Resources/medlist.txt` (31,229 medications)
  - Copied into Docker image during build
  - Read-only, cannot be modified at runtime
  - Used as initial seed data
  
- **Persistent Volume**: `/app/Data/medlist.txt` (user modifications)
  - Created on first run by copying from embedded resource
  - User additions/deletions persist here
  - Requires Koyeb volume mounted at `/app/Data`

### Code Flow
1. On startup, `GetMedList()` checks `/app/Data/medlist.txt`
2. If found → load from persistent volume (includes user changes)
3. If not found → load from embedded resource at `/app/Resources/medlist.txt`
4. Copy embedded to persistent volume for future modifications
5. All `+medication` and `-medication` operations save to `/app/Data/medlist.txt`

---

## Cost Summary

### Current Costs
- **Koyeb Hosting**: $0/month (free tier)
- **Domain**: $0 (using auto-generated Koyeb URL)
- **Total**: $0/month

### With Custom Domain
- **Koyeb Hosting**: $0/month (free tier)
- **Domain**: ~$10-15/year ÷ 12 = ~$1/month
- **Total**: ~$1/month

### Multiple Apps, One Domain
- **Koyeb Hosting**: $0/month (free tier for each app)
- **Domain**: ~$10-15/year (one time)
- **Subdomains**: $0 (unlimited free DNS records)
- **Total**: ~$1/month for unlimited apps

---

## Next Session Quick Start

### Check Deployment Status
1. Visit: `https://[your-service].koyeb.app`
2. Check diagnostic: `https://[your-service].koyeb.app/MedList/DiagnosticInfo`
3. Review logs in Koyeb dashboard

### Add/Modify Medications
- Add to master list: Type `+medication name` and press Add
- Remove from master list: Type `-medication name` and press Add
- Add to session: Type `medication name` and press Add (no symbol)
- Clear session: Type `cls` and press Add

### Git Workflow
```bash
git status                    # Check changes
git add -A                    # Stage all changes
git commit -m "description"   # Commit changes
git push                      # Push to GitHub (triggers Koyeb redeploy)
```

---

## Important Notes

- ✅ Medications now load successfully from embedded resource
- ⚠️ Volume NOT yet configured - user additions won't persist across restarts
- ✅ Code ready for persistent volume at `/app/Data`
- ✅ Docker build includes full medication list (31,229 items)
- 🎯 Next critical step: Add volume in Koyeb dashboard
