<?php
error_reporting(0);
$con = mysqli_connect('localhost', 'username', 'password', 'chatdb');
       
	$name = $_POST['name'];

	$query = "SELECT * FROM `users` WHERE name = '". $name ."'";
    $result = mysqli_query($con, $query);
    $count = mysqli_num_rows($result);  
	
    if($count > 0)
    {
		$row = mysqli_fetch_assoc($result);
		if($row['profilepic'] === NULL)
		{
			$deletequery = "DELETE FROM `users` WHERE name = '". $name ."'";
			$execute = mysqli_query($con, $deletequery);
			if($execute)
			{
				echo "Deleted";
			}
		}
    }
?>
