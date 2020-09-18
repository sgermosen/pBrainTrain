class Question{

 int _id ;

 String _questionant ;

 String _imagenUrl;

 String _answerRestrospective ;

 int _categoryId ;

// Category _category ;

// List<Choise> _choises ;

Question(this._questionant,this._imagenUrl,this._answerRestrospective, this._categoryId);
Question.WithId(this._id, this._questionant,this._imagenUrl,this._answerRestrospective, this._categoryId);

int get id=>_id ;

String get questionant =>_questionant ;

String get imagenUrl =>_imagenUrl;

String  get answerRestrospective =>_answerRestrospective ;

int get categoryId =>_categoryId ;

set questionant(String value){
  _questionant=value;
}

set imagenUrl(String value){
  _imagenUrl=value;
}

set answerRestrospective(String value){
  _answerRestrospective=value;
}

set categoryId(int value){
  _categoryId=value;
}

Map<String,dynamic> toMap(){
  var map = Map<String,dynamic>();
  map["questionant"] = _questionant;
  map["imagenUrl"] = _imagenUrl;
  map["answerRestrospective"] = _answerRestrospective;
  map["categoryId"] = _categoryId;
  if(_id != null){
    map["id"]=_id;
  }
  return map;
}

Question.fromObject(dynamic o)
{
  this._questionant = o["questionant"]   ;
  this._imagenUrl = o["imagenUrl"]   ;
  this._answerRestrospective = o["answerRestrospective"] ;
  this._categoryId = o["categoryId"] ;
  this._id = o["id"];
}



}