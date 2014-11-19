function(doc, meta) { 
    if (doc.type == "track" && doc.imei && doc.date_key && doc.status == 1) { 
        emit([doc.imei, doc.date_key], null); 
    } 
}