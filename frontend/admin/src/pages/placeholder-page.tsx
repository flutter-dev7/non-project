export function PlaceholderPage({ title }: { title: string }) {
  return (
    <div>
      <h1 className="text-2xl font-extrabold text-stone-900">{title}</h1>
      <p className="mt-2 text-stone-500">Раздел в разработке.</p>
    </div>
  )
}