# TourShuttle

מערכת להזמנת מקומות בהסעות מאורגנות למקומות הקדושים: הכותל המערבי, עיר דוד, מגדל דוד ועוד. זה השרת, ASP.NET Core Web API על .NET 10 עם PostgreSQL. הקליינט (React) נמצא בריפו נפרד: [TourShuttle-Client](https://github.com/Yaffi-Tikochinsky/TourShuttle-Client).

## תיאור המערכת
- **מנהל** מגדיר יעדים, תחנות איסוף ונהגים. הוא יוצר הסעות: יעד, שעת יציאה, מספר מקומות, מחיר, ומסלול של תחנות לפי סדר.
- **לקוח** מחפש הסעה לפי העיר שלו ולפי היעד, בוחר תחנה ומספר מקומות, ומזמין. הוא יכול לבטל עד היציאה.
- **נהג** רואה את ההסעות שהוקצו לו, ובכל אחת את הנוסעים לפי תחנות.
- **זמן איסוף משוער לכל תחנה** מחושב לפי שעת היציאה והעומס הצפוי: דקה אחת בין תחנות בלילה (23:00 עד 07:30), שלוש בשעות העומס (14:30 עד 17:30), ושתיים בשאר היום. אפשר לקבוע להסעה מרווח משלה. הטווחים מוגדרים ב-`appsettings.json`, בשעון ישראל, ונכונים גם בטווח שעובר את חצות.

## המשאב המוגבל והטיפול בתחרות
המשאב המוגבל הוא **המקומות בהסעה**. כמה לקוחות יכולים להזמין את המקומות האחרונים באותו רגע.

- **אסימון גרסה.** לכל הסעה יש `Version` (Guid) שמסומן כ-concurrency token. הוא מתחדש בכל שמירה.
- **בדיקה ושינוי בשמירה אחת.** הזמנה קוראת את ההסעה, בודקת שיש מספיק מקומות, מורידה אותם, ושומרת באותה יחידת עבודה. ה-`UPDATE` כולל את ה-`Version` שנקרא. אם מישהו אחר שמר את ההסעה בינתיים, השורה לא מתעדכנת ושום דבר לא נשמר.
- **ניסיון חוזר או 409.** Infrastructure מתרגם את `DbUpdateConcurrencyException` ל-`ConcurrencyConflictException`. השירות מנסה שוב עד 3 פעמים, כל פעם עם קריאה טרייה. אם עדיין אין מקום, מוחזר **409 Conflict** עם הסבר. ביטול הזמנה וביטול הסעה עובדים באותה דרך.
- **אינדקסים ייחודיים** שומרים מפני כפילויות שנוצרות באותו רגע: הזמנה פעילה אחת ללקוח בהסעה, אימייל ייחודי, ושמות ייחודיים ליעדים ולתחנות.
- **הבדיקה** `TourConcurrencyTests` מריצה שני DbContext-ים שקוראים את אותה הסעה ממסד SQLite. השמירה הראשונה מצליחה, והשנייה, שמבוססת על קריאה ישנה, נדחית. את הדחייה הזו השירות הופך לניסיון חוזר או ל-409, ו-`BookingServiceTests` בודקות את זה.

## ארכיטקטורה (Clean Architecture)
| פרויקט | תפקיד | תלוי ב- |
|---|---|---|
| `TourShuttle.Domain` | ישויות ו-enums בלבד, בלי ספריות | — |
| `TourShuttle.Application` | ממשקים (repositories, `IUnitOfWork`), DTOs, שירותים, פרופילי AutoMapper, `Result` | Domain |
| `TourShuttle.Infrastructure` | EF Core: `AppDbContext`, קונפיגורציות, repositories, migrations, seed, JWT, גיבוב סיסמאות | Application, Domain |
| `TourShuttle.API` | controllers, middleware, `Program.cs` | Application (ו-Infrastructure רק לרישום ב-DI) |
| `TourShuttle.Tests` | xUnit + Moq לשירותים, SQLite בזיכרון לבדיקות מול מסד אמיתי | כולם |

**כללים:**
- controller לא נוגע ב-DbContext, אלא רק בשירות.
- ישויות לא יוצאות מה-API: כל קלט ופלט עובר דרך DTO.
- async עד הסוף, עם `CancellationToken` בכל שכבה.
- כל הזמנים ב-UTC. ההמרה לשעון ישראל נעשית רק בחישוב הטווחים ובתצוגה.

**סדר ה-middleware:**
1. `CorrelationId`: מזהה לכל בקשה. הוא מופיע בכל שורת לוג, ב-header `X-Correlation-Id` ובכל שגיאה.
2. `RequestLogging`
3. `UseCors`
4. `ExceptionHandling`: כל חריגה הופכת ל-ProblemDetails עם `correlationId`.
5. `UseStatusCodePages`
6. Authentication
7. Authorization
8. Controllers

## Endpoints
התיעוד המלא, עם כל קודי התשובה, נמצא ב-Swagger (`/swagger`). שם יש כפתור Authorize שאליו מדביקים את ה-token.

| Endpoint | מי | מה |
|---|---|---|
| `POST /api/auth/register`, `POST /api/auth/login` | כולם | הרשמה (תמיד כלקוח) והתחברות, מחזירים JWT |
| `GET /api/tours`, `GET /api/tours/{id}` | כולם | הסעות עם סינון לפי עיר, יעד ותאריכים, מיון ו-pagination; פרטי הסעה עם זמני איסוף משוערים |
| `POST/PUT/DELETE /api/tours/{id}` | Admin | יצירה ועריכה. יעד, שעה ומסלול ננעלים אחרי הזמנה ראשונה. מחיקה רק להסעה בלי הזמנות |
| `POST /api/tours/{id}/cancel` | Admin | ביטול הסעה וכל ההזמנות הפעילות בה, בטרנזקציה אחת |
| `GET /api/tours/{id}/passengers` | Admin, הנהג המשובץ | נוסעים לפי תחנות (נהג אחר מקבל 403) |
| `POST /api/tours/{id}/bookings` | Customer | הזמנת מקומות (409 כשאין מקום) |
| `GET/DELETE /api/bookings/{id}`, `GET /api/users/me/bookings` | הלקוח עצמו או Admin | הזמנה, ביטול, וההזמנות שלי |
| `GET /api/drivers/me/tours` | Driver | ההסעות של הנהג |
| `GET/POST/PUT/DELETE /api/destinations` | קריאה לכולם, כתיבה ל-Admin | יעדים |
| `GET/POST/PUT/DELETE /api/pickup-points`, `GET /api/pickup-points/cities` | Admin (ערים: כולם) | תחנות איסוף והערים שבהן יש תחנות |
| `GET /api/users`, `POST /api/users/drivers` | Admin | משתמשים לפי role, והוספת נהג |
| `GET /health` | כולם | בדיקת חיים ל-Render |

## הרצה מקומית
### דרישות
- .NET 10 SDK
- Docker Desktop (להרצת PostgreSQL 16)

### שלבים
1. הרמת PostgreSQL 16 ב-Docker (פעם אחת):
   ```bash
   docker run --name tourshuttle-pg -e POSTGRES_PASSWORD=<password> -p 127.0.0.1:5432:5432 -v tourshuttle-pgdata:/var/lib/postgresql/data -d postgres:16
   ```
2. הגדרת הסודות ב-User Secrets, משורש ה-repo. הם נשמרים מחוץ לריפו ולא נכנסים ל-git:
   ```bash
   dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=TourShuttleDb;Username=postgres;Password=<password>" --project src/TourShuttle.API
   dotnet user-secrets set "Jwt:Key" "<מחרוזת אקראית של 32 תווים לפחות>" --project src/TourShuttle.API
   ```
   ואופציונלי, מפתח הרישיון של AutoMapper (בלעדיו מופיעה אזהרה בלוג):
   ```bash
   dotnet user-secrets set "AutoMapper:LicenseKey" "<המפתח>" --project src/TourShuttle.API
   ```
3. הרצת השרת:
   ```bash
   dotnet run --project src/TourShuttle.API
   ```
   בעלייה השרת מריץ את כל ה-migrations ומכניס נתוני דמו למסד ריק, בלי שום פעולה ידנית. Swagger זמין בכתובת `http://localhost:5282/swagger`.

### נתוני הדמו (seed)
- 3 יעדים: הכותל המערבי, עיר דוד, מגדל דוד.
- 5 תחנות איסוף ב-5 ערים.
- הסעות בשעות שונות, כדי להדגים את טווחי הזמן.
- הסעה אחת עם 3 מקומות, להדגמת 409.

## משתמשי דמו
| Role | אימייל | סיסמה |
|---|---|---|
| Admin | `admin@tourshuttle.demo` | `Admin123!` |
| Customer | `customer1@tourshuttle.demo`, `customer2@tourshuttle.demo` | `Customer123!` |
| Driver | `driver1@tourshuttle.demo`, `driver2@tourshuttle.demo` | `Driver123!` |

## Migrations
1. `Init`: המודל הראשוני.
2. `AddTourMinutesOverride`: מרווח דקות בין תחנות לכל הסעה.
3. `AddPickupPointCity`: עיר לכל תחנה, עם מילוי הנתונים הקיימים מתוך השם.
4. `AddUniqueCatalogNames`: שמות ייחודיים ליעדים ולתחנות.

הוספת migration:
```bash
dotnet ef migrations add <Name> -p src/TourShuttle.Infrastructure -s src/TourShuttle.API
```

## בדיקות
```bash
dotnet test
```
97 בדיקות. השירותים נבדקים עם Moq, והחלקים שתלויים במסד הנתונים נבדקים מול SQLite בזיכרון. ביניהן:
- **concurrency:** שני DbContext-ים מתחרים על אותם מקומות.
- **זמני איסוף:** כולל טווח שעובר את חצות.
- **הזמנה וביטול,** עריכת הסעה נעולה, ביטול הסעה, וסינון לפי עיר.
- **הרשאות:** נהג לא משובץ מקבל 403.
- **הרשמה והתחברות,** ושמות כפולים.

GitHub Actions מריץ build ובדיקות על כל push.

## לוגים (NLog)
- הלוג נכתב לקונסול ולקובץ יומי (`logs/`). כל שורה כוללת את ה-CorrelationId של הבקשה.
- **רמות:** Information לכל בקשה, Warning לכל התנגשות על מקומות, Error לכל חריגה, ו-Debug לפרטי חישוב בפיתוח.
- **לא נרשמים:** סיסמאות, tokens וגוף של בקשות הרשמה והתחברות.

## CORS
הקליינט רץ בכתובת אחרת מהשרת, ולכן הדפדפן מבקש מהשרת אישור לפני כל קריאה. רק הכתובות שב-`Cors:AllowedOrigins` מקבלות אישור:
- **בפיתוח:** `appsettings.Development.json` מאשר את שרתי Vite: `localhost:5173` (dev) ו-`localhost:4173` (preview).
- **ב-Render:** משתנה הסביבה `Cors__AllowedOrigins__0` מכיל את כתובת ה-Static Site של הקליינט. אם יש עוד כתובות, מוסיפים `__1`, `__2` וכן הלאה.
- **בעלייה** השרת כותב ללוג אילו כתובות מאושרות, או אזהרה אם אין אף אחת.

## פריסה ל-Render
השרת נבנה מה-`Dockerfile` ורץ כ-Web Service עם Render PostgreSQL. משתני הסביבה:

| משתנה | ערך |
|---|---|
| `ConnectionStrings__Default` | ה-connection string של מסד הנתונים ב-Render |
| `Jwt__Key` | מחרוזת אקראית ארוכה (Generate ב-Render) |
| `Cors__AllowedOrigins__0` | כתובת הקליינט, בלי `/` בסוף |
| `AutoMapper__LicenseKey` | מפתח הרישיון של AutoMapper |
| `PORT` | `8080` |

ה-Health Check Path הוא `/health`.
