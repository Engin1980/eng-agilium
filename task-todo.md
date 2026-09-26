# Task TODO — postřehy k řešení mimo aktuální implementační plán

Věci, kterých jsem si všiml při práci na `tasks.md`/`task-stories.md`, ale nejsou přímo součástí
zadaného kroku, který se zrovna implementoval. Nejde o rozpad práce (ten je v `tasks.md`/
`task-stories.md`), ale o odložené poznámky k prověření/rozhodnutí.

- [ ] **`Agilium.Be/appsettings.Development.json`** obsahuje natvrdo commitnuté reálné SMTP
      přihlašovací údaje (`AppSettings:Email:Smtp:Username`/`Password`) — reálná emailová schránka a
      heslo v gitu. Doporučení: přesunout do `.env`/user-secrets, vyjmout z historie repozitáře a
      heslo k danému účtu rotovat.
- [ ] Root `docker-compose.yml` (`be` služba) používá `ASPNETCORE_ENVIRONMENT=Development`, takže se
      i v kontejneru natáhne `appsettings.Development.json` vč. výše zmíněných SMTP údajů a Turnstile
      je vypnutý (`Turnstile.Enabled: false`) — vhodné pro lokální vývoj, ale je potřeba promyslet
      samostatnou konfiguraci pro cokoliv blížícího se produkčnímu nasazení.
