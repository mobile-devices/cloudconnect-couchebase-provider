function(doc, meta) { 
	 if (doc.type == "notification" && doc.key && doc.dropped == false) { 
		 emit([doc.key], doc); 
	 } 
 }