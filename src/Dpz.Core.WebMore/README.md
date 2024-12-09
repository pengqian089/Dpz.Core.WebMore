### nginx
```conf
server {
    listen                      80;
    listen                      443 ssl http2;
    server_name                 www.dpangzi.com;
    ssl_certificate             /home/ubuntu/cert/more/dpangzi.com_bundle.pem;
    ssl_certificate_key         /home/ubuntu/cert/more/dpangzi.com.key;
    ssl_protocols               TLSv1.1 TLSv1.2 TLSv1.3;
    ssl_ciphers                 EECDH+CHACHA20:EECDH+CHACHA20-draft:EECDH+AES128:RSA+AES128:EECDH+AES256:RSA+AES256:EECDH+3DES:RSA+3DES:!MD5;
    ssl_prefer_server_ciphers   on;
    ssl_session_cache           shared:SSL:10m;
    ssl_session_timeout         10m;
    add_header                  Strict-Transport-Security "max-age=31536000";
    error_page 497              https://$host$request_uri;
    #SSL-END
    root                        /home/ubuntu/program/more/wwwroot;
    location / {
        root                    /home/ubuntu/program/more/wwwroot;
        try_files               $uri $uri/ /index.html = 404;
        limit_req               zone=one burst=60 nodelay;
    }
  }
```

### web.config

```xml
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
    <system.webServer>
        <httpProtocol>
            <customHeaders>
                <remove name="X-Powered-By"/>
            </customHeaders>
        </httpProtocol>
        <staticContent>
            <remove fileExtension=".blat"/>
            <remove fileExtension=".dat"/>
            <remove fileExtension=".dll"/>
            <remove fileExtension=".json"/>
            <remove fileExtension=".wasm"/>
            <remove fileExtension=".woff"/>
            <remove fileExtension=".woff2"/>
            <mimeMap fileExtension=".blat" mimeType="application/octet-stream"/>
            <mimeMap fileExtension=".dll" mimeType="application/octet-stream"/>
            <mimeMap fileExtension=".dat" mimeType="application/octet-stream"/>
            <mimeMap fileExtension=".json" mimeType="application/json"/>
            <mimeMap fileExtension=".wasm" mimeType="application/wasm"/>
            <mimeMap fileExtension=".woff" mimeType="application/font-woff"/>
            <mimeMap fileExtension=".woff2" mimeType="application/font-woff"/>
        </staticContent>
        <httpCompression>
            <dynamicTypes>
                <add mimeType="application/octet-stream" enabled="true"/>
                <add mimeType="application/wasm" enabled="true"/>
            </dynamicTypes>
        </httpCompression>
        <rewrite>[PRIVATE.md](..%2F..%2FPRIVATE.md)
            <rules>
                <rule name="Serve subdir">
                    <match url=".*"/>
                    <action type="Rewrite" url="wwwroot\{R:0}"/>
                </rule>
                <rule name="SPA fallback routing" stopProcessing="true">
                    <match url=".*"/>
                    <conditions logicalGrouping="MatchAll">
                        <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true"/>
                    </conditions>
                    <action type="Rewrite" url="wwwroot\"/>
                </rule>
                <rule name="ToHTTPS" enabled="false">
                    <match url="(.*)"/>
                    <conditions>
                        <add input="{HTTPS}" pattern="^OFF$"/>
                    </conditions>
                    <action type="Rewrite" url="https://{HTTP_HOST}/{R:1}"/>
                </rule>
            </rules>
        </rewrite>
    </system.webServer>
</configuration>

```