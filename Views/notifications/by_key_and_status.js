function(doc, meta) { 
	 if (doc.type == "notification" && doc.key && doc.status == 0) { 
		 emit(doc.key, null); 
	 } 
 }