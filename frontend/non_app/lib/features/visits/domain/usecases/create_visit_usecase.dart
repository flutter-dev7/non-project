import '../repository/visits_repository.dart';

class CreateVisitUseCase {
  const CreateVisitUseCase(this._repository);

  final VisitsRepository _repository;

  Future<void> call({required String shopId, String? note}) {
    return _repository.createVisit(shopId: shopId, note: note);
  }
}