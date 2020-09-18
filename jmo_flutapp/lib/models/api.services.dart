import 'package:http/http.dart' as http;

class APIServices{
  static String questionUrl='http://192.0.0.1:5005/api/Question';

  static Future fetchQuestion() async {
    return await http.get(questionUrl);
  }
}