<?php
	error_reporting(0);
	$con = mysqli_connect('localhost', 'username', 'password', 'chatdb');
       
	$name = $_POST['name'];
	$password = $_POST['password'];
	$version = $_POST['v'];

	if($version != 1)
	{
		echo "wv";
		return;
	}
	$query = "SELECT * FROM `users` WHERE name = '". $name ."'";
    $result = mysqli_query($con, $query);
    $count = mysqli_num_rows($result);  
	
    if($count > 0)
    {
		$row = mysqli_fetch_assoc($result);
		$fetch_pass = $row['password'];
        if(password_verify($password, $fetch_pass)){
			echo $row['profilepic'] . "|" . $row['id'] . "|" . $row['name'] . "|" . $row['publickey']. "|" . $row['privatekey'];   
		}
    }
?>
