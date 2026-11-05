export function App() {
  return (
    <main className="min-h-screen bg-slate-50 text-slate-900 flex flex-col items-center justify-center p-6">
      <div className="max-w-md w-full bg-white rounded-2xl shadow-sm border border-slate-200 p-8 text-center">
        <h1 className="text-2xl font-bold tracking-tight text-slate-900 mb-2">
          Virtual Wardrobe
        </h1>
        <p className="text-sm text-slate-500 mb-6">
          Цифровий гардероб та інтелектуальний підбір аутфітів.
        </p>
        <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full text-xs font-medium bg-emerald-50 text-emerald-700 border border-emerald-200">
          <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
          Frontend (React + Vite + Tailwind) готовий до роботи
        </div>
      </div>
    </main>
  )
}

export default App
