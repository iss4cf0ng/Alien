<?php
@ini_set('display_error', '0');
@set_time_limit(0);

if (extension_loaded('openssl')) {
    echo("OpenSSL");
}
if (extension_loaded("odbc")) {
    echo("ODBC");
}
if (extension_loaded("sqlsrv")) {

}
if (extension_loaded("pgsql")) {

}
if (extension_loaded("redis")) {
    
}
if (extension_loaded("sqlite3")) {

}


?>