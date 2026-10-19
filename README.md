# TourShuttle

מערכת להזמנת מקומות בהסעות מאורגנות לאתרי טיול וסיור (הכותל המערבי, עיר דוד ועוד). השרת בנוי ב-ASP.NET Core Web API (.NET 10) עם PostgreSQL.

## תיאור המערכת
_יושלם בהמשך._

## המשאב המוגבל והטיפול בתחרות
_יושלם בשלב 5: איזה משאב מוגבל, איפה התחרות עליו, ואיך המערכת מגיבה (409)._

## ארכיטקטורה
_יושלם: מבנה Clean Architecture וההבדל מהמבנה Core / Data / Service / API._

## הרצה מקומית
### דרישות
- .NET 10 SDK
- Docker Desktop (להרצת PostgreSQL 16)

### שלבים
1. הרמת PostgreSQL 16 ב-Docker (פעם אחת):
   ```bash
   docker run --name tourshuttle-pg -e POSTGRES_PASSWORD=<password> -p 127.0.0.1:5432:5432 -v tourshuttle-pgdata:/var/lib/postgresql/data -d postgres:16
   ```
2. הגדרת ה-connection string ב-User Secrets (משורש ה-repo):
   ```bash
   dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=TourShuttleDb;Username=postgres;Password=<password>" --project src/TourShuttle.API
   ```
3. הרצת השרת:
   ```bash
   dotnet run --project src/TourShuttle.API
   ```
   בעלייה השרת מריץ את כל ה-migrations ומכניס נתוני דמו לבסיס נתונים ריק, כך שאין צורך בשום פעולה ידנית בבסיס הנתונים. Swagger זמין בכתובת `http://localhost:5282/swagger`.

## משתמשי דמו
_יושלם בשלב 3: משתמש לכל role (Admin / Customer / Driver)._
