function(doc, meta) { 
	 if (doc.type == "notification" && doc.dropped) { 
	     emit([doc.dropped], doc); 
	 } 
 }