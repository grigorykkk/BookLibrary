import SwiftUI

struct GenreFormView: View {
    let title: String
    let genre: Genre?
    let onSave: (GenreRequest) async -> Bool

    @Environment(\.dismiss) private var dismiss

    @State private var name: String
    @State private var description: String
    @State private var localErrorMessage: String?
    @State private var isSaving = false

    init(title: String, genre: Genre?, onSave: @escaping (GenreRequest) async -> Bool) {
        self.title = title
        self.genre = genre
        self.onSave = onSave

        _name = State(initialValue: genre?.name ?? "")
        _description = State(initialValue: genre?.description ?? "")
    }

    var body: some View {
        NavigationStack {
            Form {
                Section("Данные жанра") {
                    TextField("Название", text: $name)
                        .onChange(of: name) { _, newValue in
                            let filtered = String(newValue.filter { $0.isLetter || $0 == "-" || $0 == " " }.prefix(100))
                            if filtered != newValue { name = filtered }
                        }
                    TextField("Описание", text: $description, axis: .vertical)
                        .lineLimit(3...6)
                }

                if let localErrorMessage {
                    Section {
                        Text(localErrorMessage)
                            .foregroundStyle(.red)
                    }
                }
            }
            .navigationTitle(title)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Отмена") {
                        dismiss()
                    }
                }

                ToolbarItem(placement: .confirmationAction) {
                    Button(isSaving ? "Сохранение..." : "Сохранить") {
                        Task {
                            await save()
                        }
                    }
                    .disabled(isSaving)
                }
            }
        }
    }

    private func save() async {
        localErrorMessage = nil

        let normalizedName = name.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !normalizedName.isEmpty else {
            localErrorMessage = "Название обязательно."
            return
        }

        let normalizedDescription = description.trimmingCharacters(in: .whitespacesAndNewlines)

        let request = GenreRequest(
            name: normalizedName,
            description: normalizedDescription.isEmpty ? nil : normalizedDescription)

        isSaving = true
        let isSaved = await onSave(request)
        isSaving = false

        if isSaved {
            dismiss()
        }
    }
}
