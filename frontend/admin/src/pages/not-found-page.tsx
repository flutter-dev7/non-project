import { ArrowLeft, Home, Wheat } from 'lucide-react'
import { useNavigate } from 'react-router-dom'

import { Button } from '@/components/ui/button'

export function NotFoundPage() {
  const navigate = useNavigate()

  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-[#FFF8EE] p-6">
      <div className="pointer-events-none absolute -right-24 -top-24 size-96 rounded-full bg-[#C8801A]/15 blur-3xl" />
      <div className="pointer-events-none absolute -bottom-32 -left-20 size-96 rounded-full bg-[#7A4E16]/10 blur-3xl" />
      <Wheat className="pointer-events-none absolute -bottom-16 -right-10 size-[26rem] rotate-12 text-[#C8801A]/10" />

      <div className="relative text-center">
        <div className="mx-auto mb-6 flex size-16 items-center justify-center rounded-2xl bg-gradient-to-br from-[#7A4E16] to-[#C8801A] text-white shadow-lg shadow-amber-900/20">
          <Wheat className="size-8" />
        </div>

        <p
          className="bg-gradient-to-br from-[#7A4E16] to-[#C8801A] bg-clip-text text-[9rem] leading-none text-transparent sm:text-[11rem]"
          style={{ fontFamily: "'Playfair Display', serif", fontWeight: 900 }}
        >
          404
        </p>

        <h1 className="mt-4 text-3xl font-extrabold text-[#3B2A1A]">Страница не найдена</h1>
        <p className="mx-auto mt-2 max-w-md text-stone-500">
          Такой страницы нет или она была перемещена. Проверьте адрес или вернитесь на главную.
        </p>

        <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
          <Button
            variant="outline"
            className="h-11 rounded-xl bg-white px-5 font-bold"
            onClick={() => navigate(-1)}
          >
            <ArrowLeft className="size-4" />
            Назад
          </Button>
          <Button className="h-11 rounded-xl px-5 font-bold" onClick={() => navigate('/')}>
            <Home className="size-4" />
            На главную
          </Button>
        </div>
      </div>
    </div>
  )
}