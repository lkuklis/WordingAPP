import WordingKit

/// Grade presentation: one source of button labels and action identifiers, shared by
/// the notification, the menu bar and the word list. UI text does not belong in
/// WordingKit, which is why this extension lives in the app layer.
extension ReviewGrade {
    /// Presentation order - the same everywhere.
    static let ordered: [ReviewGrade] = [.good, .hard, .again]

    var buttonTitle: String {
        switch self {
        case .good: "I know it"
        case .hard: "Hard"
        case .again: "Don't know"
        }
    }

    var actionIdentifier: String {
        switch self {
        case .good: "wording.grade.good"
        case .hard: "wording.grade.hard"
        case .again: "wording.grade.again"
        }
    }

    init?(actionIdentifier: String) {
        guard let match = Self.ordered.first(where: { $0.actionIdentifier == actionIdentifier })
        else { return nil }

        self = match
    }
}
