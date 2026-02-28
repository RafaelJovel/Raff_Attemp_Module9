# API Key Setup - Quick Start Guide

## Choose Your Preferred Method

You have **three options** for setting up your Anthropic API key. Pick whichever fits your workflow best:

---

## ⚡ Option A: Local Config File (Recommended for Beginners)

**Pros:** Simple, visible, persists across sessions
**Setup Time:** ~30 seconds

```bash
# 1. Copy the template
cp src/FeatureAssessment.Core/appsettings.Development.template.json \
   src/FeatureAssessment.Core/appsettings.Development.local.json

# 2. Edit the file and replace YOUR_API_KEY_HERE with your actual key
# 3. Run the app - it just works!

dotnet run --project src/FeatureAssessment.Core
```

**Why it's safe:**
- File is automatically `.gitignore`d
- Can't accidentally commit it
- Template shows you exactly what to add

---

## 🔒 Option B: User Secrets (Recommended for Security)

**Pros:** Most secure, stored outside project directory
**Setup Time:** ~1 minute

```bash
# 1. Initialize User Secrets
dotnet user-secrets init --project src/FeatureAssessment.Core

# 2. Set your API key
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-api03-YOUR-KEY" \
  --project src/FeatureAssessment.Core

# 3. Verify it's set (optional)
dotnet user-secrets list --project src/FeatureAssessment.Core

# 4. Run the app - it just works!
dotnet run --project src/FeatureAssessment.Core
```

**Why it's safe:**
- Stored in `~/.microsoft/usersecrets/` (outside project)
- Zero chance of accidental commit
- .NET standard approach

---

## 🌍 Option C: Environment Variable (Production-Like)

**Pros:** Works everywhere, CI/CD standard
**Setup Time:** ~10 seconds

```bash
# Linux/macOS
export ANTHROPIC_API_KEY=sk-ant-api03-YOUR-KEY

# Windows PowerShell
$env:ANTHROPIC_API_KEY="sk-ant-api03-YOUR-KEY"

# Windows Command Prompt
set ANTHROPIC_API_KEY=sk-ant-api03-YOUR-KEY

# Run the app - it just works!
dotnet run --project src/FeatureAssessment.Core
```

**Why it's safe:**
- Not stored in files
- Easy to change without editing code
- Standard for production deployments

---

## 📋 What If I Set Multiple?

**Priority Order** (highest priority wins):
1. 🌍 Environment Variable
2. 🔒 User Secrets
3. ⚡ Local Config File (`*.local.json`)
4. 📄 Base Config (empty/placeholder)

**Example:** If you have both a User Secret AND a local config file, the User Secret wins.

---

## 🎯 Which Should I Choose?

| Method | Best For | Setup | Security |
|--------|----------|-------|----------|
| **Local Config File** | Beginners, quick prototyping | ⚡ Fastest | ✅ Safe (gitignored) |
| **User Secrets** | .NET developers, long-term use | 🔒 Standard | ✅✅ Safest (outside repo) |
| **Environment Variable** | CI/CD, production, multiple projects | 🌍 Universal | ✅ Safe (not in files) |

**My recommendation:** Start with **Option A** (local config file) to get running fast, then switch to **Option B** (User Secrets) once comfortable.

---

## 🚨 Troubleshooting

### Error: "Anthropic API key is required"

**Check all three locations:**

```bash
# 1. Check environment variable
echo $ANTHROPIC_API_KEY           # Linux/macOS
echo %ANTHROPIC_API_KEY%          # Windows CMD
$env:ANTHROPIC_API_KEY            # Windows PowerShell

# 2. Check User Secrets
dotnet user-secrets list --project src/FeatureAssessment.Core

# 3. Check local config file
cat src/FeatureAssessment.Core/appsettings.Development.local.json  # Linux/macOS
type src\FeatureAssessment.Core\appsettings.Development.local.json  # Windows
```

### Error: "Authentication failed"

- Verify your API key format: Should start with `sk-ant-api03-`
- Check your key is valid: https://console.anthropic.com

### Error: "Seeing someone else's API key in config"

- You're probably looking at the **template file** (`.template.json`)
- Copy it to `.local.json` and edit that instead
- Never edit the template file directly

---

## 🎓 How It Works

```
Configuration Loading Order:
┌─────────────────────────────────────┐
│  appsettings.json                   │  Base defaults (empty API key)
└──────────────┬──────────────────────┘
               │ Merged with ↓
┌─────────────────────────────────────┐
│  appsettings.Development.json       │  Environment-specific defaults
└──────────────┬──────────────────────┘
               │ Merged with ↓
┌─────────────────────────────────────┐
│  appsettings.Development.local.json │  ⚡ Your local overrides (gitignored)
└──────────────┬──────────────────────┘
               │ Overridden by ↓
┌─────────────────────────────────────┐
│  User Secrets                       │  🔒 Your secrets (outside repo)
└──────────────┬──────────────────────┘
               │ Overridden by ↓
┌─────────────────────────────────────┐
│  Environment Variables              │  🌍 Runtime configuration (highest priority)
└─────────────────────────────────────┘
```

**Result:** You get the flexibility to use whichever method you prefer, and they all "just work"!

---

## 📚 Get Your API Key

1. Sign up: https://console.anthropic.com
2. Navigate to: API Keys section
3. Create a new key
4. Copy it (starts with `sk-ant-api03-`)
5. Use any method above to set it up

**Cost:** Claude Haiku 4.5 costs ~$0.01 for a full test suite run. Very affordable for development!

---

## ✅ Verify It's Working

```bash
# Run the test harness
dotnet run --project tests/FeatureAssessment.TestHarness

# You should see:
# ✅ Anthropic client initialized
# ✅ Model: claude-haiku-4-5
# ✅ Ready to process queries
```

**Success!** You're all set. Now you can develop without worrying about API keys.
