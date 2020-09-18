 import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:jmo_flutapp/models/api.services.dart';
import 'package:jmo_flutapp/models/question.dart';

class Questions extends  StatefulWidget{
   Questions ({Key key}):super(key:key);
   @override 
   State<StatefulWidget>  createState()=> _QuestionState();
 }

 class _QuestionState extends State<Questions>{
List<Question> questions ;

getQuestion()
{
  APIServices.fetchQuestion()
  .then((response){
Iterable list = json.decode(response.body);
List<Question> questionList = List<Question>();
questionList=list.map((model ) 
=> Question.fromObject(model)).toList(); 

setState((){
questions = questionList;
});
  });
}
@override 
Widget build(BuildContext context)
{
  return Scaffold(
    appBar: AppBar(
      title:Text('QuestionsList')       ,
      )            ,
      body: null,
      );
}
 
 }