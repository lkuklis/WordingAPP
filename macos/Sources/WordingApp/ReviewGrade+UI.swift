import WordingKit

/// Prezentacja ocen: jedno zrodlo etykiet i identyfikatorow akcji, z ktorego
/// korzystaja powiadomienie, menu w pasku i okno z lista. Tekst UI nie nalezy
/// do WordingKit, dlatego rozszerzenie zyje w warstwie aplikacji.
extension ReviewGrade {
    /// Kolejnosc prezentacji - ta sama wszedzie.
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
