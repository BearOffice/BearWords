# Bear Words
**Bear Words** is a cross-platform dictionary app with tagging support, dark mode, and export/import capabilities.   
This app features a sync system backed by a lightweight self-hosted server, 
allowing sharing of bookmarks across devices.

The project already includes a set of words in en, ja, ru, and zh-hans.
Translations are provided in en, ja, and zh-hans.

See `DatabaseMigration/data/README.md` for details of dictionary source.

## ✨ Features
* Bookmark words and phrases with tag support
* Sync bookmarks across devices via self-hosted server
* Dark mode support
* Import/export data (e.g., bookmarks, phrases and tags)
* Cross-platform support:

  * Server: Windows, Docker (ASP.NET Core Container)
  * App: Windows, Android
  * *iOS/macOS app is theoretically supported but not published due to Apple Developer account requirements ($99 per year by the way)*
 
### Screenshots
<img height="300" alt="image" src="https://github.com/user-attachments/assets/bdb838f2-928a-457f-ba49-f9715f00e79f" />

<img height="300" alt="image" src="https://github.com/user-attachments/assets/4da3a586-9877-4bdd-9797-cbff41ec914a" />

<img height="300" alt="image" src="https://github.com/user-attachments/assets/bd1cf459-a1ca-4ac7-b0bb-4b31fe47f735" />

<img height="300" alt="image" src="https://github.com/user-attachments/assets/965c08d2-8d51-4177-9afa-7c6c0ab004e8" />

<img height="300" alt="image" src="https://github.com/user-attachments/assets/46f67c11-2950-4fd7-8fcc-2b07ee8a2066" />

<img height="300" alt="image" src="https://github.com/user-attachments/assets/cdb240be-0937-46ff-9005-2ca6b32c78df" />

## 🚀 Run the server
To use Bear Words—**even without syncing**—you must run the server at least once to initialize and sync dictionary data.

### Option 1: Basic One-Time Setup (Windows Only)
Recommended for single-device initialization use.

1. Download:

   * `server x64.zip` (server)
   * `bear_words.db` and place it in `data/` under the server directory
   * `server_configs.txt` and place it in the server directory
2. Edit `server_configs.txt`:

   * Add a 256-bit `issuer_key` (you can generate one online)
   * Add or modify users
3. Run `BearWordsAPI.exe`
4. The server will be available at `http://<your-server-ip>:8080`

> [!NOTE]
> The `server_configs.txt` file uses the **Bear Markup Language** syntax.  
> You can find its grammar and structure in the [the documentation](https://github.com/BearOffice/BearMarkupLanguage/wiki/Bear-Markup-Language-ver-5.0#list).

### Option 2: Docker Setup (Recommended for Normal Use)
1. Copy `BearWordsAPI/docker-compose.yml` into a working directory
2. Download and place `bear_words.db` in `data/` in that directory
3. Add `server_configs.txt` to the working directory
4. Edit `server_configs.txt`:

   * Add a 256-bit `issuer_key`
   * Add or modify users
5. Start the server:

   ```bash
   docker-compose up
   ```
6. Access the app at `http://<your-server-ip>:8080`

> [!NOTE]
> To enable TLS, provide a `.pfx` certificate and uncomment the relevant lines in the Docker Compose file.

## 📲 Run the app
### Android
* Download and install the APK.
* You may need to allow installation from unknown sources.

### Windows
* Download the Windows x64 app from the release page.

### Initial Setup
1. Enter your server’s address and port (e.g., `http://192.168.1.100:8080`)
2. Enter a username (default: `admin`)
3. If sync fails at initialization, you can change the endpoint in **Settings** and retry in the **Sync** page.

## 🔧 Recommended Setup for the app
### 1. Import Word Roots (English) as Tags
1. Go to `Settings > Import`
2. Paste contents of `en_roots.txt` from the release page into the import field
3. Select data type as `Tags` and import

### 2. Import Tag Hints for Auto-Tagging
1. Go to `Settings > Import`
2. Paste contents of `tag_hints.txt` into the import field
3. Select data type as `Words` and import

See more in the *Special Functions* section below.

## ✨ Special Functions
### Tag Hints (Auto-Tagging)
When you bookmark a word, relevant tags can be automatically applied based on hints.

To add or modify tag hints:  
* Create a new phrase with the following properties:

  * Language: `@none`
  * Tag: `@Tag Hint`
  * Title: any (e.g., "Root Hint 1")
  * Content: JSON format:

    ```json
    {
      "tag": ["word1", "word2"]
    }
    ```
    
> [!NOTE]
> The tag must be created or the hint of it will be ignored.

<img height="300" alt="image" src="https://github.com/user-attachments/assets/81b5802f-3956-4d7d-a418-89cb03cbe302" />

<img height="300" alt="image" src="https://github.com/user-attachments/assets/ab530304-664f-468c-a07f-a5845bc76f85" />

## 📚 Building Your Own Dictionary
You can build and import your own dictionary to the database (`bear_words.db`)

### Schema Location

* ORM schema: `DatabaseMigration/data_context.py`
* Migration logic: `DatabaseMigration/DatabaseMigration.py`

### Process
1. Add languages to the `Language` table (used in source and translated words)
2. Insert words:

   * Source language
   * Pronunciation (optional)
   * `ModifiedAt` as a UNIX timestamp (helper function provided in `helpers.py`)
3. Insert translations:

   * Target word's ID
   * Translation language
   * `ModifiedAt`

You can find the sample data in the `DatabaseMigration/data/` directory.

## 🛠️ Building the Project
### BearWordsAPI (Server)
* Build using the Dockerfile
* Or build in Visual Studio (`Release` configuration)

### BearWordsMaui (App)
To correctly publish a release version for Windows:

```bash
dotnet publish -f net9.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win10-x64 -p:WindowsPackageType=None
```

Reference: [.NET MAUI publish documentation](https://learn.microsoft.com/ja-jp/dotnet/maui/windows/deployment/publish-unpackaged-cli?view=net-maui-9.0)
