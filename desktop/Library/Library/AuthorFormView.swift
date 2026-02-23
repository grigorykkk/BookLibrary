import SwiftUI

struct AuthorFormView: View {
    let title: String
    let author: Author?
    let onSave: (AuthorRequest) async -> Bool

    @Environment(\.dismiss) private var dismiss

    @State private var firstName: String
    @State private var lastName: String
    @State private var birthDate: Date
    @State private var country: String
    @State private var localErrorMessage: String?
    @State private var isSaving = false

    init(title: String, author: Author?, onSave: @escaping (AuthorRequest) async -> Bool) {
        self.title = title
        self.author = author
        self.onSave = onSave

        _firstName = State(initialValue: author?.firstName ?? "")
        _lastName = State(initialValue: author?.lastName ?? "")
        _birthDate = State(initialValue: author.flatMap { DateOnlyFormatter.date(from: $0.birthDate) } ?? Date())
        _country = State(initialValue: author?.country ?? "")
    }

    var body: some View {
        NavigationStack {
            Form {
                Section("Данные автора") {
                    TextField("Имя", text: $firstName)
                    TextField("Фамилия", text: $lastName)
                    DatePicker("Дата рождения", selection: $birthDate, displayedComponents: .date)
                    TextField("Страна", text: $country)
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

        let normalizedFirstName = firstName.trimmingCharacters(in: .whitespacesAndNewlines)
        let normalizedLastName = lastName.trimmingCharacters(in: .whitespacesAndNewlines)

        guard !normalizedFirstName.isEmpty else {
            localErrorMessage = "Имя обязательно."
            return
        }

        guard !normalizedLastName.isEmpty else {
            localErrorMessage = "Фамилия обязательна."
            return
        }

        let normalizedCountry = country.trimmingCharacters(in: .whitespacesAndNewlines)

        let request = AuthorRequest(
            firstName: normalizedFirstName,
            lastName: normalizedLastName,
            birthDate: DateOnlyFormatter.string(from: birthDate),
            country: normalizedCountry.isEmpty ? nil : normalizedCountry)

        isSaving = true
        let isSaved = await onSave(request)
        isSaving = false

        if isSaved {
            dismiss()
        }
    }
}
