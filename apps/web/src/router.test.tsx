import { render, screen } from '@testing-library/react'
import { RouterProvider } from '@tanstack/react-router'
import { router } from './router'

describe('router', () => {
  it('renders the homepage at the index route', async () => {
    window.history.replaceState(null, '', '/')

    render(<RouterProvider router={router} />)

    expect(
      await screen.findByRole('heading', {
        name: /verder trainen als een apparaat niet beschikbaar is/i,
      }),
    ).toBeInTheDocument()
  })
})
