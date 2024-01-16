<?php
	
    $wp = $_GET['wp'];
  
    function title($url)
    {
        $cont = file_get_contents($url);
        if ($cont === false)
	    {
            return null;
	    }

        $result = preg_match("/<title>(.*)<\/title>/siU", $cont, $match);
        
        if ($result === false)
	    {
            return null; 
	    }
        if(sizeof($match) === 0)
	    {
            return null;
	    }

        return $match[1];
    }

    $test = title($wp);
    if($test === null)
    {
         $pieces = explode("/",$wp);
         $wp = explode(".",$pieces[2]);
         echo $pieces[3] . " - " . ucfirst($wp[1]);
    }
    else
    {
        echo $test;
    }
?>

