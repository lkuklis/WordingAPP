import WordingKit

/// Turns the typed errors from WordingKit into something to read.
///
/// The wording lives here rather than in the package on purpose: WordingKit raises
/// typed cases and the UI decides how to say them, so the same error can read one way
/// in a dialog and another in a log.
extension WordPackError {
    var readableMessage: String {
        switch self {
        case .notHttps:
            "The address has to start with https://"
        case .network(let detail):
            "Could not download the pack. \(detail)"
        case .tooLarge:
            "That file is too big to be a word pack."
        case .malformed(let detail):
            "That file is not a word pack. \(detail)"
        case .empty:
            "The pack has no words in it."
        case .unsafeId:
            "The pack has an identifier Wording cannot use as a file name."
        case .alreadyExists:
            "You already have this pack."
        }
    }
}

extension Error {
    /// Falls back to the system description for anything that is not a pack error.
    var packMessage: String {
        (self as? WordPackError)?.readableMessage ?? localizedDescription
    }
}
