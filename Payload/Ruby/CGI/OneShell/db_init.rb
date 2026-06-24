def main
  checks = {
    'MySQLi' => false,
    'MySQL' => false,
    'PDO' => false,
    'PDO MySQL' => false,
    'PDO PostgreSQL' => false,
    'PDO SQLite' => false,
    'PostgreSQL' => false,
    'SQLite3' => true,
    'Redis' => false,
    'MongoDB' => false,
    'Oracle (OCI8)' => false,
    'Microsoft SQL Server' => true,
    'ODBC' => true
  }

  checks.each do |db, available|
    status = available ? 1 : 0
    print "#{db}:#{status},"
  end

  return
end

main()