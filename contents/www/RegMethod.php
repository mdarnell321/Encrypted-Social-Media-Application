<?php
	error_reporting(1);
	$con = mysqli_connect('localhost', 'username', 'password', 'chatdb');
       
	$name = $_POST['name'];
    $password = $_POST['pass'];
    $publickey = $_POST['publickey'];
    $privatekey = $_POST['privatekey'];

	$query = "SELECT * FROM `users` WHERE name = '". $name ."'";
    $result = mysqli_query($con, $query);
    $count = mysqli_num_rows($result);  
	
    if($count > 0)
    {
		echo "Username already taken";
		return;
    }
    $encpass = password_hash($password, PASSWORD_BCRYPT);
	$insert_data = "INSERT INTO users (name,servers,publickey, privatekey, password) values('$name', '', '$publickey', '$privatekey', '$encpass')";
    $data_check = mysqli_query($con, $insert_data);
    if($data_check){
        echo "Success";
    }else{
        echo "Failed inserting data into database!";
    }
?>
