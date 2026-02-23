import SwiftUI

struct AuthorsView: View {
    @ObservedObject var store: LibraryStore

    @State private var isPresentingCreateAuthor = false
    @State private var editingAuthor: Author?
    @State private var deletingAuthor: Author?

    var body: some View {
        NavigationStack {
            List(store.authors) { author in
                VStack(alignment: .leading, spacing: 4) {
                    Text(author.fullName)
                        .font(.headline)

                    Text("Дата рождения: \(author.birthDate)")
                        .font(.footnote)
                        .foregroundStyle(.secondary)

                    if let country = author.country, !country.isEmpty {
                        Text("Страна: \(country)")
                            .font(.footnote)
                            .foregroundStyle(.secondary)
                    }
                }
                .contextMenu {
                    Button("Редактировать") {
                        editingAuthor = author
                    }

                    Button("Удалить", role: .destructive) {
                        deletingAuthor = author
                    }
                }
            }
            .overlay {
                if store.authors.isEmpty {
                    ContentUnavailableView("Нет авторов", systemImage: "person.2")
                }
            }
            .navigationTitle("Authors")
            .toolbar {
                ToolbarItem(placement: .automatic) {
                    Button("Обновить") {
                        Task {
                            await store.refreshReferences()
                        }
                    }
                }

                ToolbarItem(placement: .primaryAction) {
                    Button("Добавить") {
                        isPresentingCreateAuthor = true
                    }
                }
            }
            .sheet(isPresented: $isPresentingCreateAuthor) {
                AuthorFormView(title: "Новый автор", author: nil) { request in
                    await store.createAuthor(request: request)
                }
            }
            .sheet(item: $editingAuthor) { author in
                AuthorFormView(title: "Редактировать автора", author: author) { request in
                    await store.updateAuthor(id: author.id, request: request)
                }
            }
            .alert("Удалить автора?", isPresented: Binding(
                get: { deletingAuthor != nil },
                set: { isPresented in
                    if !isPresented {
                        deletingAuthor = nil
                    }
                }))
            {
                Button("Удалить", role: .destructive) {
                    guard let deletingAuthor else {
                        return
                    }

                    Task {
                        _ = await store.deleteAuthor(id: deletingAuthor.id)
                        self.deletingAuthor = nil
                    }
                }

                Button("Отмена", role: .cancel) {
                    deletingAuthor = nil
                }
            } message: {
                Text("Если у автора есть книги, сервер вернёт ошибку.")
            }
        }
    }
}
