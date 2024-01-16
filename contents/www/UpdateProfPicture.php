<?php
	error_reporting(0);
	$con = mysqli_connect('localhost', 'username', 'password', 'chatdb');
       
	$name = $_POST['name'];
	$md5 = $_POST['md5'];

	$query = "SELECT * FROM `users` WHERE name = '". $name ."'";
    $result = mysqli_query($con, $query);
    $count = mysqli_num_rows($result);  
	
    if($count > 0)
    {
		 $update_pass = "UPDATE users SET profilepic = '$md5' WHERE name = '$name'";
         if(mysqli_query($con, $update_pass))
		 {
			 echo "Success";
			 return;
		 }
		 else
		 {
			 echo "Could not upload profile picture (2).";
			 return;
		 }
    }
	 echo "Could not upload profile picture.";
?>
