import 'package:flutter/widgets.dart';
import 'package:url_launcher/url_launcher.dart';

import 'app_snackbar.dart';

Future<void> callPhone(BuildContext context, String phone) async {
  var opened = false;

  try {
    opened = await launchUrl(Uri(scheme: 'tel', path: phone));
  } catch (_) {
    opened = false;
  }

  if (!opened && context.mounted) {
    AppSnackBar.show(context, 'Не удалось открыть набор номера', isError: true);
  }
}